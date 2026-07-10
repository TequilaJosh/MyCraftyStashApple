using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>One step line in the pending/saved card-build summary.</summary>
public class BuildSummaryLine
{
    public string Label { get; set; } = "";
    public bool IsInside { get; set; }   // inside-of-card steps render muted + indented
}

/// <summary>A row in the read-only "Items Used" panel (edit mode).</summary>
public class UsedItemRow
{
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Add/edit a project — clone of the desktop's inline Add/Edit panels:
/// Images card + Card Builder on the left, Name*/Notes + pending-build summary
/// in the middle, read-only "Items Used" on the right (edit only). Items are
/// linked exclusively through the Card Builder wizard, matching the desktop.
/// </summary>
public partial class ProjectEditViewModel : ObservableObject
{
    private readonly ProjectService _service;
    private readonly CardBuildService _cardService;
    private readonly AppNavigator _nav;
    private int _id;

    // Pending card build (saved together with the project, desktop parity)
    private string? _pendingCardBaseType;
    private List<WizardBuildStep>? _pendingBuildSteps;
    private string? _pendingStateSnapshot;
    private List<int> _pendingItemIds = new();

    public ProjectEditViewModel(ProjectService service, CardBuildService cardService, AppNavigator nav)
    {
        _service = service;
        _cardService = cardService;
        _nav = nav;
        Name = "";
    }

    [ObservableProperty] public partial string PageTitle { get; set; } = "Add Project";
    [ObservableProperty] public partial bool IsEditing { get; set; }
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string SaveButtonText { get; set; } = "Save Project";

    // Images: base64 data URIs; first one is the primary (Project.ImageUrl)
    public ObservableCollection<string> Images { get; } = new();
    [ObservableProperty] public partial bool HasImages { get; set; }

    // Pending / saved card build summary
    [ObservableProperty] public partial bool HasPendingCardBuild { get; set; }
    [ObservableProperty] public partial bool HasSavedCardBuild { get; set; }
    public ObservableCollection<BuildSummaryLine> PendingBuildStepList { get; } = new();
    public ObservableCollection<BuildSummaryLine> SavedBuildStepList { get; } = new();

    // Read-only "Items Used" panel (edit mode)
    public ObservableCollection<UsedItemRow> ItemsUsed { get; } = new();
    [ObservableProperty] public partial bool HasItemsUsed { get; set; }

    private string? _savedSnapshot;   // snapshot already in the DB (edit mode)
    private string? _projectImageForWizard;

    public async void Init(int id)
    {
        if (id <= 0) return;
        _id = id;
        IsEditing = true;
        PageTitle = "Edit Project";
        SaveButtonText = "Save Changes";

        var p = await _service.GetAsync(id);
        if (p is null) return;
        Name = p.Name;
        Notes = p.Notes;
        _projectImageForWizard = p.ImageUrl;

        Images.Clear();
        if (!string.IsNullOrEmpty(p.ImageUrl)) Images.Add(p.ImageUrl);
        foreach (var img in await _service.GetProjectImagesAsync(id))
            Images.Add(img.ImageUrl);
        HasImages = Images.Count > 0;

        ItemsUsed.Clear();
        foreach (var pi in p.ProjectItems.OrderBy(x => x.SortOrder))
            if (pi.Item is not null)
                ItemsUsed.Add(new UsedItemRow { ItemId = pi.Item.Id, Name = pi.Item.Name, ImageUrl = pi.Item.ImageUrl });
        HasItemsUsed = ItemsUsed.Count > 0;

        var build = await _cardService.GetForProjectAsync(id);
        if (build is not null)
        {
            _savedSnapshot = build.StateSnapshot;
            SavedBuildStepList.Clear();
            foreach (var s in build.Steps)
                SavedBuildStepList.Add(new BuildSummaryLine { Label = s.Label, IsInside = s.Section == "inside" });
            HasSavedCardBuild = SavedBuildStepList.Count > 0;
        }
    }

    // ── Images ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddImages()
    {
        try
        {
            var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
            {
                PickerTitle = "Choose project photos",
                FileTypes = FilePickerFileType.Images,
            });
            foreach (var file in results)
            {
                using var stream = await file.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                var ext = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
                var mime = ext switch
                {
                    "jpg" or "jpeg" => "image/jpeg",
                    "gif" => "image/gif",
                    "bmp" => "image/bmp",
                    _ => "image/png",
                };
                Images.Add($"data:{mime};base64,{Convert.ToBase64String(ms.ToArray())}");
            }
            HasImages = Images.Count > 0;
        }
        catch (Exception)
        {
            // Picker cancelled or unavailable — nothing to add.
        }
    }

    [RelayCommand]
    private void RemoveImage(string image)
    {
        Images.Remove(image);
        HasImages = Images.Count > 0;
    }

    // ── Card Builder (the only item-linking mechanism, desktop parity) ───────

    [RelayCommand]
    private async Task OpenCardBuilder()
    {
        var vm = new CardWizardViewModel();
        var snapshot = _pendingStateSnapshot ?? _savedSnapshot;
        await vm.InitializeAsync(_id, string.IsNullOrWhiteSpace(Name) ? "New project" : Name,
            Images.FirstOrDefault() ?? _projectImageForWizard, snapshot);

        var page = new Views.CardWizardPage(vm, wiz =>
        {
            if (!wiz.WasConfirmed) return;

            _pendingCardBaseType = wiz.CardBaseType;
            _pendingBuildSteps = wiz.BuildSteps.ToList();
            _pendingStateSnapshot = wiz.CaptureSnapshotJson();
            _pendingItemIds = wiz.SelectedItemIds.Distinct().ToList();

            PendingBuildStepList.Clear();
            foreach (var s in _pendingBuildSteps)
                PendingBuildStepList.Add(new BuildSummaryLine { Label = s.Label, IsInside = s.Section == "inside" });
            HasPendingCardBuild = PendingBuildStepList.Count > 0;

            // Desktop appends the wizard's notes to the project notes.
            if (!string.IsNullOrWhiteSpace(wiz.BuildOtherNotes))
                Notes = string.IsNullOrWhiteSpace(Notes) ? wiz.BuildOtherNotes : $"{Notes}\n{wiz.BuildOtherNotes}";

            _ = RefreshItemsUsedFromPending();
        });

        var nav = Application.Current?.Windows[0].Page?.Navigation;
        if (nav is not null)
            await nav.PushModalAsync(page);
    }

    /// <summary>Merges wizard-selected items into the Items Used display list.</summary>
    private async Task RefreshItemsUsedFromPending()
    {
        var known = ItemsUsed.Select(r => r.ItemId).ToHashSet();
        var newIds = _pendingItemIds.Where(id => !known.Contains(id)).ToList();
        if (newIds.Count == 0) { HasItemsUsed = ItemsUsed.Count > 0; return; }

        var inventory = new InventoryService();
        foreach (var id in newIds)
        {
            var item = await inventory.GetItemAsync(id);
            if (item is not null)
                ItemsUsed.Add(new UsedItemRow { ItemId = item.Id, Name = item.Name, ImageUrl = item.ImageUrl });
        }
        HasItemsUsed = ItemsUsed.Count > 0;
    }

    // ── Save / Cancel ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Project name is required"; return; }

        string? primary = Images.FirstOrDefault();
        var extras = Images.Skip(1).ToList();

        if (_id > 0)
        {
            var p = await _service.GetAsync(_id);
            if (p is null) { _nav.Back(); return; }
            p.Name = Name.Trim();
            p.Notes = Blank(Notes);
            p.ImageUrl = primary;
            await _service.UpdateAsync(p);
        }
        else
        {
            _id = await _service.AddAsync(new Project
            {
                Name = Name.Trim(),
                Notes = Blank(Notes),
                ImageUrl = primary,
            });
        }

        await _service.ReplaceProjectImagesAsync(_id, extras);

        if (_pendingItemIds.Count > 0)
            await _service.MergeItemsIntoProjectAsync(_id, _pendingItemIds);

        if (_pendingBuildSteps is { Count: > 0 } && _pendingCardBaseType is not null)
            await _cardService.SaveWizardBuildAsync(_id, _pendingCardBaseType, _pendingBuildSteps, _pendingStateSnapshot);

        _nav.Back();
    }

    [RelayCommand]
    private void Cancel() => _nav.Back();

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
