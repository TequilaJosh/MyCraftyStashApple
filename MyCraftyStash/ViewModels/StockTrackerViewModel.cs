using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>
/// Stock tracker — clone of the desktop's two-tab view:
///  • Supply Stock — tracked-type items grouped by type with inline stock edit
///  • Project Inventory — finished-card on-hand counts per project, inline edit
/// Search per tab, success/error banners, one-row-at-a-time editing.
/// </summary>
public partial class StockTrackerViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InventoryService _service;
    private readonly AppNavigator _nav;

    private List<StockRow> _allSupplyRows = new();
    private List<ProjectStockRow> _allProjectRows = new();

    public StockTrackerViewModel(InventoryService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
    }

    // Tabs
    [ObservableProperty] public partial string ActiveTab { get; set; } = "Supply";
    public bool IsSupplyTab => ActiveTab == "Supply";
    public bool IsProjectsTab => ActiveTab == "Projects";
    partial void OnActiveTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsSupplyTab));
        OnPropertyChanged(nameof(IsProjectsTab));
    }

    [RelayCommand] private void ShowSupplyTab() => ActiveTab = "Supply";
    [RelayCommand] private void ShowProjectsTab() => ActiveTab = "Projects";

    // Data
    public ObservableCollection<StockGroup> StockGroups { get; } = new();
    public ObservableCollection<ProjectStockRow> ProjectRows { get; } = new();

    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [ObservableProperty] public partial bool SupplyEmpty { get; set; }
    [ObservableProperty] public partial bool ProjectsEmpty { get; set; }
    [ObservableProperty] public partial string? SuccessMessage { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    // Search
    [ObservableProperty] public partial string? SearchText { get; set; }
    [ObservableProperty] public partial string? ProjectSearchText { get; set; }
    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnProjectSearchTextChanged(string? value) => ApplyProjectFilter();

    public Task Refresh() => Load();

    [RelayCommand]
    public async Task Load()
    {
        Busy = true;
        ErrorMessage = null;
        try
        {
            var items = await _service.GetTrackedSupplyItemsAsync();
            _allSupplyRows = items.Select(i => new StockRow
            {
                ItemId = i.Id,
                Name = i.Name,
                Type = i.Type,
                Subtype = i.Subtype,
                PackSize = i.PackSize,
                CurrentStock = i.CurrentStock ?? 0,
                EditValueText = (i.CurrentStock ?? 0).ToString(),
            }).ToList();
            ApplyFilter();

            var projects = await _service.GetTrackedProjectsAsync();
            _allProjectRows = projects.Select(p => new ProjectStockRow
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                QuantityOnHand = p.QuantityOnHand ?? 0,
                EditValueText = (p.QuantityOnHand ?? 0).ToString(),
            }).ToList();
            ApplyProjectFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
        finally { Busy = false; IsRefreshing = false; }
    }

    [RelayCommand] private Task DoRefresh() => Load();

    private void ApplyFilter()
    {
        var s = SearchText?.Trim();
        IEnumerable<StockRow> rows = _allSupplyRows;
        if (!string.IsNullOrEmpty(s))
            rows = rows.Where(r =>
                r.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                r.Type.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                (r.Subtype != null && r.Subtype.Contains(s, StringComparison.OrdinalIgnoreCase)));

        StockGroups.Clear();
        foreach (var g in rows.GroupBy(r => r.Type).OrderBy(g => g.Key))
        {
            var group = new StockGroup { TypeName = g.Key };
            foreach (var r in g.OrderBy(r => r.Name)) group.Items.Add(r);
            group.RecomputeTotal();
            StockGroups.Add(group);
        }
        SupplyEmpty = StockGroups.Count == 0;
    }

    private void ApplyProjectFilter()
    {
        var s = ProjectSearchText?.Trim();
        IEnumerable<ProjectStockRow> rows = _allProjectRows;
        if (!string.IsNullOrEmpty(s))
            rows = rows.Where(r => r.ProjectName.Contains(s, StringComparison.OrdinalIgnoreCase));

        ProjectRows.Clear();
        foreach (var r in rows) ProjectRows.Add(r);
        ProjectsEmpty = ProjectRows.Count == 0;
    }

    // ── Supply inline edit ────────────────────────────────────────────────────

    [RelayCommand]
    private void StartEditStock(StockRow row)
    {
        foreach (var g in StockGroups) foreach (var r in g.Items) r.IsEditing = false;
        row.EditValueText = row.CurrentStock.ToString();
        row.IsEditing = true;
    }

    [RelayCommand]
    private void CancelEditStock(StockRow row) => row.IsEditing = false;

    [RelayCommand]
    private async Task SaveEditStock(StockRow row)
    {
        if (!int.TryParse(row.EditValueText?.Trim(), out int value) || value < 0)
        {
            ErrorMessage = "Enter a whole number of 0 or more.";
            return;
        }
        try
        {
            await _service.UpdateItemStockAsync(row.ItemId, value);
            row.CurrentStock = value;
            row.IsEditing = false;
            foreach (var g in StockGroups) if (g.Items.Contains(row)) g.RecomputeTotal();
            await FlashSuccess($"Updated {row.Name} stock to {value} {row.UnitLabel}");
        }
        catch (Exception ex) { ErrorMessage = $"Failed to save: {ex.Message}"; }
    }

    // ── Project inline edit ───────────────────────────────────────────────────

    [RelayCommand]
    private void StartEditProjectStock(ProjectStockRow row)
    {
        foreach (var r in ProjectRows) r.IsEditing = false;
        row.EditValueText = row.QuantityOnHand.ToString();
        row.IsEditing = true;
    }

    [RelayCommand]
    private void CancelEditProjectStock(ProjectStockRow row) => row.IsEditing = false;

    [RelayCommand]
    private async Task SaveEditProjectStock(ProjectStockRow row)
    {
        if (!int.TryParse(row.EditValueText?.Trim(), out int value) || value < 0)
        {
            ErrorMessage = "Enter a whole number of 0 or more.";
            return;
        }
        try
        {
            await _service.UpdateProjectQuantityOnHandAsync(row.ProjectId, value);
            row.QuantityOnHand = value;
            row.IsEditing = false;
            await FlashSuccess($"Updated \"{row.ProjectName}\" quantity to {value}");
        }
        catch (Exception ex) { ErrorMessage = $"Failed to save: {ex.Message}"; }
    }

    // ── Track a new project (MAUI adaptation of the desktop's Settings picker) ──

    public ObservableCollection<Project> UntrackedProjects { get; } = new();
    [ObservableProperty] public partial bool IsTrackPickerOpen { get; set; }

    [RelayCommand]
    private async Task OpenTrackPicker()
    {
        var all = await _service.GetAllProjectsForStockAsync();
        var trackedIds = _allProjectRows.Select(r => r.ProjectId).ToHashSet();
        UntrackedProjects.Clear();
        foreach (var p in all.Where(p => !trackedIds.Contains(p.Id)))
            UntrackedProjects.Add(p);
        IsTrackPickerOpen = true;
    }

    [RelayCommand]
    private void CloseTrackPicker() => IsTrackPickerOpen = false;

    [RelayCommand]
    private async Task TrackProject(Project project)
    {
        await _service.UpdateProjectQuantityOnHandAsync(project.Id, 0);
        IsTrackPickerOpen = false;
        await Load();
        ActiveTab = "Projects";
    }

    [RelayCommand]
    private async Task UntrackProject(ProjectStockRow row)
    {
        await _service.UpdateProjectQuantityOnHandAsync(row.ProjectId, null);
        await Load();
    }

    [RelayCommand]
    private void OpenItem(StockRow row) => _nav.PushDetail(row.ItemId);

    private async Task FlashSuccess(string message)
    {
        SuccessMessage = message;
        await Task.Delay(3000);
        if (SuccessMessage == message) SuccessMessage = null;
    }
}
