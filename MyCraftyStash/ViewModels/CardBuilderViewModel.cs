using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>A step type option (friendly display + persisted StepType value).</summary>
public record StepTypeOption(string Display, string Value);

/// <summary>One row in the builder's step list.</summary>
public partial class CardStepRow : ObservableObject
{
    public string Section { get; set; } = "exterior";   // "exterior" | "inside"
    public string StepType { get; set; } = "";
    [ObservableProperty] public partial string Label { get; set; } = "";
    public int? MatLayer { get; set; }
    public int? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string? CuttingMethod { get; set; }
}

/// <summary>
/// Working card builder for one project. Records the ordered build steps for the
/// exterior and inside of a card — card base, mats, focal, sentiment,
/// embellishments — each optionally linked to an inventory item and cut method.
/// Saves to / loads from the shared card-build tables via CardBuildService.
/// </summary>
public partial class CardBuilderViewModel : ObservableObject
{
    private readonly CardBuildService _service;
    private readonly InventoryService _inventory;
    private readonly AppNavigator _nav;
    private int _projectId;

    public CardBuilderViewModel(CardBuildService service, InventoryService inventory, AppNavigator nav)
    {
        _service = service;
        _inventory = inventory;
        _nav = nav;
        CardBase = CardBases[0];
        NewSection = Sections[0];
        NewStepType = StepTypes[0];
        NewCuttingMethod = CuttingMethods[0];
    }

    // ── Option lists (verbatim from the desktop) ──
    public List<string> CardBases { get; } = new()
    {
        "A2 Top Fold", "A2 Side Fold", "A7 Top Fold", "A7 Side Fold",
        "Mini Slim Top Fold", "Mini Slim Side Fold", "Fun Fold",
    };

    public List<StepTypeOption> StepTypes { get; } = new()
    {
        new("Card base", "card_base"),
        new("Cardstock", "cardstock"),
        new("Background mat", "background_mat"),
        new("Additional mat", "additional_mat"),
        new("Embossing folder", "embossing_folder"),
        new("Focal mat", "focal_mat"),
        new("Focal cardstock", "focal_cardstock"),
        new("Backer", "backer"),
        new("Sentiment", "sentiment"),
        new("Embellishment", "embellishment"),
        new("Envelope", "envelope"),
    };

    public List<string> CuttingMethods { get; } = new()
    {
        "—", "Stacklets", "All Planned Out", "Frames", "Insider", "Foil-It", "Dies", "Custom", "None",
    };

    public List<string> Sections { get; } = new() { "Exterior", "Inside" };

    // ── Header ──
    [ObservableProperty] public partial string ProjectName { get; set; } = "";
    [ObservableProperty] public partial string CardBase { get; set; }

    public ObservableCollection<CardStepRow> ExteriorSteps { get; } = new();
    public ObservableCollection<CardStepRow> InsideSteps { get; } = new();

    // ── New-step composer ──
    [ObservableProperty] public partial string NewSection { get; set; }
    [ObservableProperty] public partial StepTypeOption NewStepType { get; set; }
    [ObservableProperty] public partial string NewCuttingMethod { get; set; }
    [ObservableProperty] public partial string? NewMatLayerText { get; set; }
    [ObservableProperty] public partial string? NewLabel { get; set; }

    // Inline item picker for the composer
    [ObservableProperty] public partial string? NewItemSearch { get; set; }
    public ObservableCollection<Item> ItemCandidates { get; } = new();
    [ObservableProperty] public partial Item? SelectedNewItem { get; set; }
    public string SelectedNewItemName => SelectedNewItem?.Name ?? "";
    public bool HasSelectedNewItem => SelectedNewItem is not null;

    partial void OnSelectedNewItemChanged(Item? value)
    {
        OnPropertyChanged(nameof(SelectedNewItemName));
        OnPropertyChanged(nameof(HasSelectedNewItem));
    }

    public async void Init(int projectId, string projectName)
    {
        _projectId = projectId;
        ProjectName = projectName;
        var build = await _service.GetForProjectAsync(projectId);
        if (build is null) return;

        if (!string.IsNullOrWhiteSpace(build.CardBaseType) && CardBases.Contains(build.CardBaseType))
            CardBase = build.CardBaseType;

        ExteriorSteps.Clear();
        InsideSteps.Clear();
        foreach (var s in build.Steps)
        {
            var row = new CardStepRow
            {
                Section = s.Section,
                StepType = s.StepType,
                Label = s.Label,
                MatLayer = s.MatLayer,
                ItemId = s.ItemId,
                ItemName = s.Item?.Name,
                CuttingMethod = s.CuttingMethod,
            };
            (s.Section == "inside" ? InsideSteps : ExteriorSteps).Add(row);
        }
    }

    [RelayCommand]
    private async Task SearchItems()
    {
        var items = await _inventory.GetItemsAsync(NewItemSearch);
        ItemCandidates.Clear();
        foreach (var i in items.Take(15))
            ItemCandidates.Add(i);
    }

    [RelayCommand]
    private void PickCandidate(Item item)
    {
        SelectedNewItem = item;
        ItemCandidates.Clear();
        NewItemSearch = "";
    }

    [RelayCommand]
    private void ClearItem() => SelectedNewItem = null;

    [RelayCommand]
    private void AddStep()
    {
        bool inside = NewSection == "Inside";
        int? matLayer = int.TryParse(NewMatLayerText, out var m) ? m : null;
        string cutting = NewCuttingMethod is "—" or "None" or null ? (NewCuttingMethod == "None" ? "None" : "") : NewCuttingMethod;

        var label = string.IsNullOrWhiteSpace(NewLabel) ? BuildLabel() : NewLabel!.Trim();

        var row = new CardStepRow
        {
            Section = inside ? "inside" : "exterior",
            StepType = NewStepType.Value,
            Label = label,
            MatLayer = matLayer,
            ItemId = SelectedNewItem?.Id,
            ItemName = SelectedNewItem?.Name,
            CuttingMethod = string.IsNullOrEmpty(cutting) ? null : cutting,
        };
        (inside ? InsideSteps : ExteriorSteps).Add(row);

        // Reset the composer (keep section + step type for quick repeat)
        SelectedNewItem = null;
        NewMatLayerText = null;
        NewLabel = null;
        NewItemSearch = "";
        ItemCandidates.Clear();
    }

    private string BuildLabel()
    {
        var prefix = NewStepType.Display;
        var parts = new List<string>();
        if (SelectedNewItem is not null) parts.Add(SelectedNewItem.Name);
        if (NewCuttingMethod is not ("—" or "None")) parts.Add(NewCuttingMethod);
        return parts.Count > 0 ? $"{prefix}: {string.Join(" · ", parts)}" : prefix;
    }

    [RelayCommand]
    private void RemoveStep(CardStepRow row)
    {
        if (!ExteriorSteps.Remove(row)) InsideSteps.Remove(row);
    }

    [RelayCommand]
    private void MoveUp(CardStepRow row) => Move(row, -1);

    [RelayCommand]
    private void MoveDown(CardStepRow row) => Move(row, +1);

    private void Move(CardStepRow row, int delta)
    {
        var list = ExteriorSteps.Contains(row) ? ExteriorSteps
                 : InsideSteps.Contains(row) ? InsideSteps
                 : null;
        if (list is null) return;
        int i = list.IndexOf(row);
        int j = i + delta;
        if (j < 0 || j >= list.Count) return;
        list.Move(i, j);
    }

    [RelayCommand]
    private async Task Save()
    {
        var steps = new List<ProjectCardBuildStep>();
        foreach (var r in ExteriorSteps) steps.Add(ToStep(r));
        foreach (var r in InsideSteps) steps.Add(ToStep(r));
        await _service.SaveAsync(_projectId, CardBase, null, steps);
        _nav.Back();
    }

    private static ProjectCardBuildStep ToStep(CardStepRow r) => new()
    {
        Section = r.Section,
        StepType = r.StepType,
        MatLayer = r.MatLayer,
        ItemId = r.ItemId,
        CuttingMethod = r.CuttingMethod,
        Label = r.Label,
    };

    [RelayCommand]
    private void Cancel() => _nav.Back();
}
