using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>The inventory grid: local search + filters + card list, with add/open.</summary>
public partial class InventoryViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InventoryService _service;
    private readonly AppNavigator _nav;

    public InventoryViewModel(InventoryService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
        SearchText = string.Empty;
    }

    public ObservableCollection<Item> Items { get; } = new();
    public ObservableCollection<string> Types { get; } = new();
    /// <summary>Subtype checkboxes for the selected type (empty for "All types").</summary>
    public ObservableCollection<CheckOption> SubtypeFilters { get; } = new();

    [ObservableProperty] public partial string SearchText { get; set; }
    [ObservableProperty] public partial string? SelectedType { get; set; }
    [ObservableProperty] public partial bool NameOnly { get; set; } = true;
    [ObservableProperty] public partial bool ThemeOnly { get; set; }
    [ObservableProperty] public partial bool DiscontinuedOnly { get; set; }
    [ObservableProperty] public partial bool NoImageOnly { get; set; }
    [ObservableProperty] public partial bool HasSubtypeFilters { get; set; }
    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }
    [ObservableProperty] public partial string ResultSummary { get; set; } = "";

    // "Name only" and "Theme only" are mutually exclusive.
    partial void OnNameOnlyChanged(bool value) { if (value) ThemeOnly = false; }
    partial void OnThemeOnlyChanged(bool value) { if (value) NameOnly = false; }
    partial void OnSelectedTypeChanged(string? value) { _ = RefreshSubtypes(); _ = Load(); }
    partial void OnDiscontinuedOnlyChanged(bool value) => _ = Load();
    partial void OnNoImageOnlyChanged(bool value) => _ = Load();

    public Task Refresh() => Load();

    /// <summary>Rebuilds the subtype checkboxes for the selected type; checking
    /// any of them re-filters the grid.</summary>
    private async Task RefreshSubtypes()
    {
        SubtypeFilters.Clear();
        var type = SelectedType is null or "All types" ? null : SelectedType;
        if (type is not null)
        {
            foreach (var s in await _service.GetSubtypesForTypeAsync(type))
            {
                var opt = new CheckOption(s);
                opt.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(CheckOption.IsSelected)) _ = Load();
                };
                SubtypeFilters.Add(opt);
            }
        }
        HasSubtypeFilters = SubtypeFilters.Count > 0;
    }

    [RelayCommand]
    public async Task Load()
    {
        Busy = true;
        try
        {
            // Refresh type list (cheap, keeps the filter current).
            if (Types.Count == 0)
            {
                Types.Add("All types");
                foreach (var t in await _service.GetTypesAsync()) Types.Add(t);
            }

            string? searchMode = NameOnly ? "name" : ThemeOnly ? "theme" : null;
            string? type = SelectedType is null or "All types" ? null : SelectedType;
            var items = await _service.GetItemsAsync(SearchText, type, searchMode, DiscontinuedOnly);

            IEnumerable<Item> q = items;
            if (NoImageOnly)
                q = q.Where(i => string.IsNullOrEmpty(i.ImageUrl));
            var checkedSubs = SubtypeFilters.Where(o => o.IsSelected).Select(o => o.Label).ToList();
            if (checkedSubs.Count > 0)
                q = q.Where(i => i.Subtype is not null &&
                    checkedSubs.Any(s => i.Subtype.Contains(s, StringComparison.OrdinalIgnoreCase)));

            Items.Clear();
            foreach (var item in q)
                Items.Add(item);
            ResultSummary = $"{Items.Count} item{(Items.Count == 1 ? "" : "s")}";
        }
        finally
        {
            Busy = false;
            IsRefreshing = false;
            IsEmpty = Items.Count == 0;
        }
    }

    [RelayCommand]
    private Task DoRefresh() => Load();

    [RelayCommand]
    private Task Search() => Load();

    [RelayCommand]
    private void ToggleNoImage() => NoImageOnly = !NoImageOnly;

    [RelayCommand]
    private async Task ClearFilters()
    {
        SearchText = "";
        NameOnly = true;
        ThemeOnly = DiscontinuedOnly = NoImageOnly = false;
        SelectedType = "All types";
        SubtypeFilters.Clear();
        HasSubtypeFilters = false;
        await Load();
    }

    [RelayCommand]
    private void AddItem() => _nav.PushAddItem();

    [RelayCommand]
    private void OpenItem(Item item) => _nav.PushDetail(item.Id);
}
