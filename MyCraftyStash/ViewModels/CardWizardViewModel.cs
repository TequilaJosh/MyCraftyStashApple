using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>
/// The card build wizard — a MAUI clone of the desktop's CardBuildWizardWindow.
/// Hub-and-spoke: a central hub of section buttons; each spoke configures one
/// aspect (cardbase, mats, sentiments, embellishments, inside, envelope). The
/// heavy lifting (summary, step assembly, snapshot) lives in
/// CardWizardViewModel.Core.cs, copied verbatim from the desktop.
/// </summary>
public partial class CardWizardViewModel : ObservableObject
{
    private readonly CardLabelMappingService _labels = new();

    // ── Output (read by the caller after Create Card) ─────────────────────
    public bool WasConfirmed { get; private set; }
    public List<int> SelectedItemIds { get; private set; } = new();
    public string CardBaseType { get; private set; } = string.Empty;
    public List<WizardBuildStep> BuildSteps { get; private set; } = new();
    public string BuildOtherNotes { get; private set; } = string.Empty;

    [ObservableProperty] public partial string WizardNotes { get; set; } = string.Empty;

    // ── Project context (left column) ─────────────────────────────────────
    [ObservableProperty] public partial string ProjectName { get; set; } = "";
    [ObservableProperty] public partial string? ProjectImage { get; set; }
    public bool HasProjectImage => !string.IsNullOrEmpty(ProjectImage);
    partial void OnProjectImageChanged(string? value) => OnPropertyChanged(nameof(HasProjectImage));

    // ── Section navigation ─────────────────────────────────────────────────
    [ObservableProperty] public partial string CurrentSection { get; set; } = "Hub";

    partial void OnCurrentSectionChanged(string value)
    {
        OnPropertyChanged(nameof(IsHubActive));
        OnPropertyChanged(nameof(IsCardBaseSectionActive));
        OnPropertyChanged(nameof(IsBackgroundMatSectionActive));
        OnPropertyChanged(nameof(IsAdditionalMatSectionActive));
        OnPropertyChanged(nameof(IsFocalMatSectionActive));
        OnPropertyChanged(nameof(IsSentimentSectionActive));
        OnPropertyChanged(nameof(IsEmbellishmentsSectionActive));
        OnPropertyChanged(nameof(IsInsideCardstockSectionActive));
        OnPropertyChanged(nameof(IsAnyMatHubActive));
        OnPropertyChanged(nameof(IsBackgroundOrAdditionalMatActive));
        NotifyStepFlags();
    }

    public bool IsHubActive => CurrentSection == "Hub";
    public bool IsCardBaseSectionActive => CurrentSection == "CardBase";
    public bool IsBackgroundMatSectionActive => CurrentSection == "BackgroundMat";
    public bool IsAdditionalMatSectionActive => CurrentSection == "AdditionalMat";
    public bool IsFocalMatSectionActive => CurrentSection == "FocalMat";
    public bool IsSentimentSectionActive => CurrentSection == "Sentiment";
    public bool IsEmbellishmentsSectionActive => CurrentSection == "Embellishments";
    public bool IsInsideCardstockSectionActive => CurrentSection == "InsideCardstock";
    public bool IsAnyMatHubActive => IsBackgroundMatSectionActive || IsAdditionalMatSectionActive || IsFocalMatSectionActive;
    public bool IsBackgroundOrAdditionalMatActive => IsAnyMatHubActive;

    [ObservableProperty] public partial string CurrentCardBaseStep { get; set; } = "Hub";
    partial void OnCurrentCardBaseStepChanged(string value) => NotifyStepFlags();

    [ObservableProperty] public partial string BgMatHubStep { get; set; } = "Hub";
    partial void OnBgMatHubStepChanged(string value) => NotifyStepFlags();

    [ObservableProperty] public partial string SentimentSubStep { get; set; } = "Hub";
    partial void OnSentimentSubStepChanged(string value) => NotifyStepFlags();

    private void NotifyStepFlags()
    {
        OnPropertyChanged(nameof(IsCardBaseHubStep));
        OnPropertyChanged(nameof(IsCardBaseCardfoldStep));
        OnPropertyChanged(nameof(IsCardBaseCardstockStep));
        OnPropertyChanged(nameof(IsCardBaseDetailsStep));
        OnPropertyChanged(nameof(IsCardBaseAdhesivesStep));
        OnPropertyChanged(nameof(IsBgMatHubStep));
        OnPropertyChanged(nameof(IsBgMatCardstockStep));
        OnPropertyChanged(nameof(IsBgMatHowCutStep));
        OnPropertyChanged(nameof(IsBgMatDetailsStep));
        OnPropertyChanged(nameof(IsBgMatAdhesivesStep));
        OnPropertyChanged(nameof(IsSentimentSubStepHub));
        OnPropertyChanged(nameof(IsSentimentSubStepCardstock));
        OnPropertyChanged(nameof(IsSentimentSubStepAdhesives));
        OnPropertyChanged(nameof(IsDetailsStepActive));
    }

    public bool IsCardBaseHubStep => CurrentCardBaseStep == "Hub";
    public bool IsCardBaseCardfoldStep => CurrentCardBaseStep == "Cardfold";
    public bool IsCardBaseCardstockStep => CurrentCardBaseStep == "Cardstock";
    public bool IsCardBaseDetailsStep => CurrentCardBaseStep == "Details";
    public bool IsCardBaseAdhesivesStep => CurrentCardBaseStep == "Adhesives";
    public bool IsBgMatHubStep => BgMatHubStep == "Hub";
    public bool IsBgMatCardstockStep => BgMatHubStep == "Cardstock";
    public bool IsBgMatHowCutStep => BgMatHubStep == "HowCut";
    public bool IsBgMatDetailsStep => BgMatHubStep == "Details";
    public bool IsBgMatAdhesivesStep => BgMatHubStep == "Adhesives";
    public bool IsSentimentSubStepHub => SentimentSubStep == "Hub";
    public bool IsSentimentSubStepCardstock => SentimentSubStep == "Cardstock";
    public bool IsSentimentSubStepAdhesives => SentimentSubStep == "Adhesives";

    /// <summary>The shared Details page is "active" whenever any owner routed to it.</summary>
    public bool IsDetailsStepActive =>
        (IsCardBaseSectionActive && IsCardBaseDetailsStep) ||
        (IsAnyMatHubActive && IsBgMatDetailsStep) ||
        (IsSentimentSectionActive && SentimentSubStep == "Details") ||
        CurrentSection == "InsideDetails";

    // ── Inside mode ────────────────────────────────────────────────────────
    [ObservableProperty] public partial bool IsInsideMode { get; set; }

    [RelayCommand]
    private void ToggleInsideMode()
    {
        IsInsideMode = !IsInsideMode;
        CurrentSection = "Hub";
        NotifyInsideDoneIndicators();
    }

    // Inside-hub derived done flags (verbatim semantics from the desktop)
    public bool InsideBackgroundMatDone => BgMats.Any(g => g.IsInside && g.Pieces.Count > 0) || InsideBgMats.Count > 0;
    public bool InsideAdditionalMatDone => AdditionalMats.Any(g => g.IsInside && g.Pieces.Count > 0) || InsideAdditionalMats.Count > 0;
    public bool InsideFocalMatDone => FocalMatGroups.Any(g => g.IsInside && g.Pieces.Count > 0) || HasInsideFocalMat;
    public bool InsideSentimentDone => ConfiguredSentiments.Any(s => s.IsInside) || ConfiguredInsideSentiments.Count > 0;
    public bool InsideEmbellishmentsDone => AddedEmbellishments.Any(e => e.IsInside) || InsideAddedEmbellishments.Count > 0;

    private void NotifyInsideDoneIndicators()
    {
        OnPropertyChanged(nameof(InsideBackgroundMatDone));
        OnPropertyChanged(nameof(InsideAdditionalMatDone));
        OnPropertyChanged(nameof(InsideFocalMatDone));
        OnPropertyChanged(nameof(InsideSentimentDone));
        OnPropertyChanged(nameof(InsideEmbellishmentsDone));
    }

    [RelayCommand]
    private void BackToHub()
    {
        CurrentSection = "Hub";
        UpdateSummaryLines();
    }

    // ── Saved / done flags ─────────────────────────────────────────────────
    [ObservableProperty] public partial bool CardBaseSaved { get; set; }
    [ObservableProperty] public partial bool CardFoldSaved { get; set; }
    [ObservableProperty] public partial bool CardStockSaved { get; set; }
    [ObservableProperty] public partial bool CardBaseDetailsSaved { get; set; }
    [ObservableProperty] public partial bool CardBaseAdhesivesSaved { get; set; }
    [ObservableProperty] public partial bool BackgroundMatSaved { get; set; }
    [ObservableProperty] public partial bool AdditionalMatSaved { get; set; }
    [ObservableProperty] public partial bool FocalMatSaved { get; set; }
    [ObservableProperty] public partial bool SentimentSaved { get; set; }
    [ObservableProperty] public partial bool EmbellishmentsSaved { get; set; }
    [ObservableProperty] public partial bool EnvelopeSaved { get; set; }
    [ObservableProperty] public partial bool InsideCardstockSaved { get; set; }
    [ObservableProperty] public partial bool InsideDetailsSaved { get; set; }

    // ═══ CARD BASE ═══════════════════════════════════════════════════════

    public static readonly string[] CardFoldOptions =
    {
        "A2 Side Fold", "A2 Top Fold", "A7 Top Fold", "A7 Side Fold",
        "Mini Slim Top Fold", "Mini Slim Side Fold", "Fun Fold",
    };

    [ObservableProperty] public partial string SelectedCardBase { get; set; } = string.Empty;
    public bool HasSelectedCardBase => !string.IsNullOrEmpty(SelectedCardBase);
    partial void OnSelectedCardBaseChanged(string value) => OnPropertyChanged(nameof(HasSelectedCardBase));

    // Legacy focal-shaped holder for cardbase decorations (summary/assemble use it)
    public WizardFocalSection CardBase { get; } = new();
    public ObservableCollection<WizardDetailEntry> CardBaseAddedDetails { get; } = new();
    public ObservableCollection<WizardItemOption> CardBaseAddedAdhesives { get; } = new();
    public int CardBaseAdhesivesCount => CardBaseAddedAdhesives.Count;

    // Cardstock buckets (regular / foil / glitter). Picking one clears the others.
    public ObservableCollection<WizardItemOption> BaseCardstockRegularItems { get; } = new();
    public ObservableCollection<WizardItemOption> BaseCardstockFoilItems { get; } = new();
    public ObservableCollection<WizardItemOption> BaseCardstockGlitterItems { get; } = new();

    public WizardItemPicker BaseRegularCardstockPicker { get; } = new() { PlaceholderText = "Cardstock" };
    public WizardItemPicker BaseFoilCardstockPicker { get; } = new() { PlaceholderText = "Foil Cardstock" };
    public WizardItemPicker BaseGlitterCardstockPicker { get; } = new() { PlaceholderText = "Glitter Cardstock" };

    public WizardItemOption? SelectedBaseRegularCardstockItem { get; set; }
    public WizardItemOption? SelectedBaseFoilCardstockItem { get; set; }
    public WizardItemOption? SelectedBaseGlitterCardstockItem { get; set; }

    [ObservableProperty] public partial string? SelectedBaseCardstockColor { get; set; }
    [ObservableProperty] public partial string BaseCardstockOtherText { get; set; } = string.Empty;
    public string EffectiveBaseCardstockColor =>
        SelectedBaseCardstockColor == "Other" ? BaseCardstockOtherText : (SelectedBaseCardstockColor ?? "");

    [ObservableProperty] public partial bool BaseIsSelfBlended { get; set; }
    [ObservableProperty] public partial string BaseSelfBlendDescription { get; set; } = string.Empty;
    public InkSelection BaseBlendInks { get; } = new();

    [RelayCommand] private void NavToCardBase() { CurrentCardBaseStep = "Hub"; CurrentSection = "CardBase"; }
    [RelayCommand] private void NavCardBaseToCardfold() => CurrentCardBaseStep = "Cardfold";
    [RelayCommand] private void NavCardBaseToCardstock() => CurrentCardBaseStep = "Cardstock";
    [RelayCommand]
    private void NavCardBaseToDetails()
    {
        ClearDetailSelections();
        DetailsReturnTarget = "CardBase";
        CurrentCardBaseStep = "Details";
    }
    [RelayCommand] private void NavCardBaseToAdhesives() => CurrentCardBaseStep = "Adhesives";
    [RelayCommand] private void BackToCardBaseHub() => CurrentCardBaseStep = "Hub";

    [RelayCommand] private void SelectCardFold(string fold) => SelectedCardBase = fold;

    [RelayCommand]
    private void SaveCardFold()
    {
        CardFoldSaved = !string.IsNullOrEmpty(SelectedCardBase);
        UpdateSummaryLines();
        CurrentCardBaseStep = "Hub";
    }

    [RelayCommand]
    private void SaveCardStock()
    {
        // Mirror the picker picks into the Selected* slots (pick one clears others)
        var reg = BaseRegularCardstockPicker.SelectedItem;
        var foil = BaseFoilCardstockPicker.SelectedItem;
        var glit = BaseGlitterCardstockPicker.SelectedItem;
        var chosen = reg ?? foil ?? glit;
        SelectedBaseRegularCardstockItem = reg;
        SelectedBaseFoilCardstockItem = foil;
        SelectedBaseGlitterCardstockItem = glit;
        if (chosen is not null) SelectedBaseCardstockColor = chosen.Name;

        CardStockSaved = !string.IsNullOrEmpty(SelectedBaseCardstockColor) || BaseIsSelfBlended;
        UpdateSummaryLines();
        CurrentCardBaseStep = "Hub";
    }

    [RelayCommand]
    private void SaveCardBaseAndBackToHub()
    {
        CardBaseSaved = true;
        CurrentCardBaseStep = "Hub";
        CurrentSection = "Hub";
        UpdateSummaryLines();
    }

    // Cardbase adhesives
    public WizardItemPicker GlueAdhesivePicker { get; } = new() { PlaceholderText = "Glue" };
    public WizardItemPicker FoamAdhesivePicker { get; } = new() { PlaceholderText = "Foam" };
    public WizardItemPicker TapeRunnerAdhesivePicker { get; } = new() { PlaceholderText = "Tape Runner" };

    public bool HasCurrentCardBaseAdhesivePick =>
        GlueAdhesivePicker.SelectedItem != null || FoamAdhesivePicker.SelectedItem != null || TapeRunnerAdhesivePicker.SelectedItem != null;
    public string CurrentCardBaseAdhesivePreview => string.Join("   •   ",
        new[] { GlueAdhesivePicker.SelectedItem?.Name, FoamAdhesivePicker.SelectedItem?.Name, TapeRunnerAdhesivePicker.SelectedItem?.Name }
        .Where(n => n != null));

    [RelayCommand]
    private void SaveCardBaseAdhesives()
    {
        foreach (var pick in new[] { GlueAdhesivePicker, FoamAdhesivePicker, TapeRunnerAdhesivePicker })
        {
            if (pick.SelectedItem is { } it && CardBaseAddedAdhesives.All(a => a.Id != it.Id))
                CardBaseAddedAdhesives.Add(it);
            pick.SelectedItem = null;
        }
        OnPropertyChanged(nameof(CardBaseAdhesivesCount));
        CardBaseAdhesivesSaved = CardBaseAddedAdhesives.Count > 0;
        UpdateSummaryLines();
        CurrentCardBaseStep = "Hub";
    }

    [RelayCommand]
    private void RemoveCardBaseAdhesive(WizardItemOption item)
    {
        CardBaseAddedAdhesives.Remove(item);
        OnPropertyChanged(nameof(CardBaseAdhesivesCount));
        CardBaseAdhesivesSaved = CardBaseAddedAdhesives.Count > 0;
        UpdateSummaryLines();
    }

    // ═══ MATS (Background / Additional / Focal share one hub) ═════════════

    public ObservableCollection<WizardBgMatGroup> BgMats { get; } = new();
    public ObservableCollection<WizardBgMatGroup> AdditionalMats { get; } = new();
    public ObservableCollection<WizardBgMatGroup> FocalMatGroups { get; } = new();
    public ObservableCollection<WizardFocalSection> FocalParts { get; } = new();   // legacy (restore only)

    [ObservableProperty] public partial string CurrentMatTarget { get; set; } = "BackgroundMat";
    [ObservableProperty] public partial WizardBgMat CurrentMat { get; set; } = new();
    private WizardBgMatGroup _currentBgMatGroup = new();
    public ObservableCollection<WizardBgMat> CurrentBgMatPieces => _currentBgMatGroup.Pieces;

    [ObservableProperty] public partial bool BgPieceCardstockSaved { get; set; }
    [ObservableProperty] public partial bool BgPieceDetailsSaved { get; set; }
    [ObservableProperty] public partial bool BgPieceHowCutSaved { get; set; }
    [ObservableProperty] public partial bool BgPieceAdhesivesSaved { get; set; }

    public string MatTitle => CurrentMatTarget switch
    {
        "AdditionalMat" => "Additional Mat",
        "FocalMat" => "Focal Mat",
        _ => "Background Mat",
    };
    public string MatBackButtonLabel => CurrentMatTarget switch
    {
        "AdditionalMat" => "← Back to Additional Mat",
        "FocalMat" => "← Back to Focal Mat",
        _ => "← Back to Background Mat",
    };
    public string MatPieceActionLabel => CurrentMatTarget == "FocalMat" ? "Add 1 Part" : "Add 1 Piece of mat";

    partial void OnCurrentMatTargetChanged(string value)
    {
        OnPropertyChanged(nameof(MatTitle));
        OnPropertyChanged(nameof(MatBackButtonLabel));
        OnPropertyChanged(nameof(MatPieceActionLabel));
    }

    private void StartMatSection(string target)
    {
        CurrentMatTarget = target;
        _currentBgMatGroup = new WizardBgMatGroup
        {
            TypeLabel = target switch { "AdditionalMat" => "Additional", "FocalMat" => "Focal", _ => "Background" },
        };
        OnPropertyChanged(nameof(CurrentBgMatPieces));
        ResetCurrentMat();
        BgMatHubStep = "Hub";
        CurrentSection = target;
    }

    [RelayCommand] private void NavToBackgroundMat() => StartMatSection("BackgroundMat");
    [RelayCommand] private void NavToAdditionalMat() => StartMatSection("AdditionalMat");
    [RelayCommand] private void NavToFocalMat() => StartMatSection("FocalMat");

    [RelayCommand] private void NavBgMatToCardstock() => BgMatHubStep = "Cardstock";
    [RelayCommand] private void NavBgMatToHowCut() => BgMatHubStep = "HowCut";
    [RelayCommand]
    private void NavBgMatToDetails()
    {
        ClearDetailSelections();
        DetailsReturnTarget = CurrentMatTarget;
        BgMatHubStep = "Details";
    }
    [RelayCommand] private void NavBgMatToAdhesives() => BgMatHubStep = "Adhesives";
    [RelayCommand] private void BackToBgMatHub() => BgMatHubStep = "Hub";

    private void ResetCurrentMat()
    {
        CurrentMat = new WizardBgMat();
        BgPieceCardstockSaved = BgPieceDetailsSaved = BgPieceHowCutSaved = BgPieceAdhesivesSaved = false;
        BgPieceCardstockPicker.SelectedItem = null;
        BgPieceFoilCardstockPicker.SelectedItem = null;
        BgPieceGlitterCardstockPicker.SelectedItem = null;
        BgPieceBlendInks.Clear();
        BgCutStackletsPicker.SelectedItem = null;
        BgCutPlannedOutPicker.SelectedItem = null;
        BgCutFramesPicker.SelectedItem = null;
        BgCutInsiderPicker.SelectedItem = null;
        BgCutFoilItPicker.SelectedItem = null;
        BgCutFoilsPicker.SelectedItem = null;
        BgPieceGlueAdhesivePicker.SelectedItem = null;
        BgPieceFoamAdhesivePicker.SelectedItem = null;
        BgPieceTapeRunnerAdhesivePicker.SelectedItem = null;
        NotifyCutState();
    }

    [RelayCommand]
    private void AddBgMatPiece()
    {
        if (!string.IsNullOrEmpty(CurrentMat.DisplaySummary) && CurrentMat.DisplaySummary != "(empty)")
        {
            CurrentMat.Layer = _currentBgMatGroup.Pieces.Count + 1;
            _currentBgMatGroup.Pieces.Add(CurrentMat);
            _currentBgMatGroup.NotifyDisplaySummaryChanged();
        }
        ResetCurrentMat();
        OnPropertyChanged(nameof(CurrentBgMatPieces));
        UpdateSummaryLines();
    }

    [RelayCommand]
    private void RemoveBgMatPiece(WizardBgMat piece)
    {
        _currentBgMatGroup.Pieces.Remove(piece);
        int n = 1;
        foreach (var p in _currentBgMatGroup.Pieces) p.Layer = n++;
        _currentBgMatGroup.NotifyDisplaySummaryChanged();
        UpdateSummaryLines();
    }

    [RelayCommand]
    private void AddBgMat()
    {
        // Commit any in-progress piece first
        if (!string.IsNullOrEmpty(CurrentMat.DisplaySummary) && CurrentMat.DisplaySummary != "(empty)")
        {
            CurrentMat.Layer = _currentBgMatGroup.Pieces.Count + 1;
            _currentBgMatGroup.Pieces.Add(CurrentMat);
        }

        if (_currentBgMatGroup.Pieces.Count > 0)
        {
            _currentBgMatGroup.IsInside = IsInsideMode;
            switch (CurrentMatTarget)
            {
                case "AdditionalMat":
                    _currentBgMatGroup.GroupNumber = AdditionalMats.Count + 1;
                    AdditionalMats.Add(_currentBgMatGroup);
                    AdditionalMatSaved = true;
                    break;
                case "FocalMat":
                    _currentBgMatGroup.GroupNumber = FocalMatGroups.Count + 1;
                    FocalMatGroups.Add(_currentBgMatGroup);
                    FocalMatSaved = true;
                    break;
                default:
                    _currentBgMatGroup.GroupNumber = BgMats.Count + 1;
                    BgMats.Add(_currentBgMatGroup);
                    BackgroundMatSaved = true;
                    break;
            }
        }

        _currentBgMatGroup = new WizardBgMatGroup
        {
            TypeLabel = CurrentMatTarget switch { "AdditionalMat" => "Additional", "FocalMat" => "Focal", _ => "Background" },
        };
        ResetCurrentMat();
        OnPropertyChanged(nameof(CurrentBgMatPieces));
        NotifyInsideDoneIndicators();
        UpdateSummaryLines();
        BgMatHubStep = "Hub";
        CurrentSection = "Hub";
    }

    // Mat cardstock
    public WizardItemPicker BgPieceCardstockPicker { get; } = new() { PlaceholderText = "Cardstock" };
    public WizardItemPicker BgPieceFoilCardstockPicker { get; } = new() { PlaceholderText = "Foil Cardstock" };
    public WizardItemPicker BgPieceGlitterCardstockPicker { get; } = new() { PlaceholderText = "Glitter Cardstock" };
    public InkSelection BgPieceBlendInks { get; } = new();

    [RelayCommand]
    private void SaveBgPieceCardstock()
    {
        var pick = BgPieceCardstockPicker.SelectedItem
                   ?? BgPieceFoilCardstockPicker.SelectedItem
                   ?? BgPieceGlitterCardstockPicker.SelectedItem;
        CurrentMat.SelectedCardstockColor = pick?.Name;
        if (CurrentMat.IsSelfBlended)
        {
            CurrentMat.BlendInkColors.Clear();
            CurrentMat.BlendInkColors.AddRange(BgPieceBlendInks.Ordered);
        }
        BgPieceCardstockSaved = pick != null || CurrentMat.IsSelfBlended;
        UpdateSummaryLines();
        BgMatHubStep = "Hub";
    }

    // How was it cut
    public WizardItemPicker BgCutStackletsPicker { get; } = new() { PlaceholderText = "Stacklets" };
    public WizardItemPicker BgCutPlannedOutPicker { get; } = new() { PlaceholderText = "All Planned Out" };
    public WizardItemPicker BgCutFramesPicker { get; } = new() { PlaceholderText = "Frames" };
    public WizardItemPicker BgCutInsiderPicker { get; } = new() { PlaceholderText = "Insider" };
    public WizardItemPicker BgCutFoilItPicker { get; } = new() { PlaceholderText = "Foil-It" };
    public WizardItemPicker BgCutFoilsPicker { get; } = new() { PlaceholderText = "Foil sheet" };

    public string CurrentBgCuttingMethod => CurrentMat.CuttingMethod;
    public bool ShowCutDieIndex => CurrentMat.StackletItem != null || CurrentMat.FramesItem != null;
    public bool ShowCutLayers => CurrentMat.StackletItem != null || CurrentMat.FramesItem != null || CurrentMat.PlannedOutItem != null;
    public bool ShowCutFoils => CurrentMat.InsiderItem != null || CurrentMat.FoilItItem != null;
    public bool ShowAnyCutFollowups => ShowCutDieIndex || ShowCutLayers || ShowCutFoils;
    public string CutFollowupHeader => "Applies to the pick above";

    private void NotifyCutState()
    {
        OnPropertyChanged(nameof(CurrentBgCuttingMethod));
        OnPropertyChanged(nameof(ShowCutDieIndex));
        OnPropertyChanged(nameof(ShowCutLayers));
        OnPropertyChanged(nameof(ShowCutFoils));
        OnPropertyChanged(nameof(ShowAnyCutFollowups));
    }

    private void WireBgCutPickerEchoes()
    {
        BgCutStackletsPicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WizardItemPicker.SelectedItem)) return;
            if (BgCutStackletsPicker.SelectedItem is { } it)
            {
                CurrentMat.StackletItem = it;
                CurrentMat.CuttingMethod = "Stacklets";
            }
            else CurrentMat.StackletItem = null;
            NotifyCutState();
        };
        BgCutPlannedOutPicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WizardItemPicker.SelectedItem)) return;
            CurrentMat.PlannedOutItem = BgCutPlannedOutPicker.SelectedItem;
            if (CurrentMat.PlannedOutItem != null) CurrentMat.CuttingMethod = "All Planned Out";
            NotifyCutState();
        };
        BgCutFramesPicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WizardItemPicker.SelectedItem)) return;
            CurrentMat.FramesItem = BgCutFramesPicker.SelectedItem;
            if (CurrentMat.FramesItem != null) CurrentMat.CuttingMethod = "Frames";
            NotifyCutState();
        };
        BgCutInsiderPicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WizardItemPicker.SelectedItem)) return;
            CurrentMat.InsiderItem = BgCutInsiderPicker.SelectedItem;
            if (CurrentMat.InsiderItem != null) CurrentMat.CuttingMethod = "Insider";
            NotifyCutState();
        };
        BgCutFoilItPicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WizardItemPicker.SelectedItem)) return;
            CurrentMat.FoilItItem = BgCutFoilItPicker.SelectedItem;
            if (CurrentMat.FoilItItem != null) CurrentMat.CuttingMethod = "Foil-It";
            NotifyCutState();
        };
        BgCutFoilsPicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WizardItemPicker.SelectedItem)) return;
            CurrentMat.FoilsItem = BgCutFoilsPicker.SelectedItem;
            NotifyCutState();
        };
    }

    [RelayCommand]
    private void SelectBgCuttingMethod(string method)
    {
        CurrentMat.CuttingMethod = method;
        NotifyCutState();
    }

    [RelayCommand]
    private void SaveBgPieceHowCut()
    {
        BgPieceHowCutSaved = !string.IsNullOrEmpty(CurrentMat.CuttingMethod);
        UpdateSummaryLines();
        BgMatHubStep = "Hub";
    }

    // Mat adhesives
    public WizardItemPicker BgPieceGlueAdhesivePicker { get; } = new() { PlaceholderText = "Glue" };
    public WizardItemPicker BgPieceFoamAdhesivePicker { get; } = new() { PlaceholderText = "Foam" };
    public WizardItemPicker BgPieceTapeRunnerAdhesivePicker { get; } = new() { PlaceholderText = "Tape Runner" };

    public bool HasCurrentBgPieceAdhesivePick =>
        BgPieceGlueAdhesivePicker.SelectedItem != null || BgPieceFoamAdhesivePicker.SelectedItem != null || BgPieceTapeRunnerAdhesivePicker.SelectedItem != null;
    public string CurrentBgPieceAdhesivePreview => string.Join("   •   ",
        new[] { BgPieceGlueAdhesivePicker.SelectedItem?.Name, BgPieceFoamAdhesivePicker.SelectedItem?.Name, BgPieceTapeRunnerAdhesivePicker.SelectedItem?.Name }
        .Where(n => n != null));

    [RelayCommand]
    private void SaveBgPieceAdhesives()
    {
        foreach (var p in new[] { BgPieceGlueAdhesivePicker, BgPieceFoamAdhesivePicker, BgPieceTapeRunnerAdhesivePicker })
        {
            if (p.SelectedItem is { } it && !CurrentMat.Adhesives.Contains(it.Name))
                CurrentMat.Adhesives.Add(it.Name);
            p.SelectedItem = null;
        }
        BgPieceAdhesivesSaved = CurrentMat.Adhesives.Count > 0;
        UpdateSummaryLines();
        BgMatHubStep = "Hub";
    }

    // ═══ DETAILS (shared page) ═════════════════════════════════════════════

    public WizardItemPicker StampsPicker { get; } = new() { PlaceholderText = "Stamps" };
    public WizardItemPicker DiesPicker { get; } = new() { PlaceholderText = "Dies" };
    public WizardItemPicker StencilsPicker { get; } = new() { PlaceholderText = "Stencils" };
    public WizardItemPicker EmbellishmentsPicker { get; } = new() { PlaceholderText = "Embellishments" };
    public WizardItemPicker StackletsPicker { get; } = new() { PlaceholderText = "Stacklets" };
    public WizardItemPicker EmbossingFoldersPicker { get; } = new() { PlaceholderText = "Embossing Folders" };
    public WizardItemPicker FoilsPicker { get; } = new() { PlaceholderText = "Foils" };
    public WizardItemPicker OloMarkersPicker { get; } = new() { PlaceholderText = "OLO Markers", IsMultiSelect = true };
    public WizardItemPicker WatercolorsPicker { get; } = new() { PlaceholderText = "Watercolors" };

    [ObservableProperty] public partial string DetailsReturnTarget { get; set; } = "CardBase";

    partial void OnDetailsReturnTargetChanged(string value)
    {
        OnPropertyChanged(nameof(DetailsBackButtonLabel));
        OnPropertyChanged(nameof(DetailsReturnButtonLabel));
        OnPropertyChanged(nameof(AddedDetailsForCurrentTarget));
    }

    public string DetailsBackButtonLabel => DetailsReturnTarget switch
    {
        "CardBase" => "← Back to Cardbase",
        "BackgroundMat" => "← Back to Background Mat",
        "AdditionalMat" => "← Back to Additional Mat",
        "FocalMat" => "← Back to Focal Mat",
        "Sentiment" => "← Back to Sentiment",
        "Inside" or "InsideMisc" => "← Back to Inside",
        _ => "← Back",
    };
    public string DetailsReturnButtonLabel => DetailsReturnTarget switch
    {
        "CardBase" => "Save & Return to Cardbase",
        "BackgroundMat" => "Save & Return to Background Mat",
        "AdditionalMat" => "Save & Return to Additional Mat",
        "FocalMat" => "Save & Return to Focal Mat",
        "Sentiment" => "Save & Return to Sentiment",
        "Inside" or "InsideMisc" => "Save & Return to Inside",
        _ => "Save & Return",
    };

    private readonly ObservableCollection<WizardDetailEntry> _emptyDetails = new();
    private readonly List<WizardDetailEntry> _pendingSentimentDetails = new();
    public ObservableCollection<WizardDetailEntry> InsideMiscDetails { get; } = new();

    public ObservableCollection<WizardDetailEntry> AddedDetailsForCurrentTarget => DetailsReturnTarget switch
    {
        "CardBase" => CardBaseAddedDetails,
        "BackgroundMat" or "AdditionalMat" or "FocalMat" => CurrentMat.AddedDetails,
        "Sentiment" => new ObservableCollection<WizardDetailEntry>(_pendingSentimentDetails),
        "InsideMisc" => InsideMiscDetails,
        _ => CardBaseAddedDetails,
    };

    // Ink / watercolor composite
    [ObservableProperty] public partial bool InksWatercolorsIsOpen { get; set; }
    [ObservableProperty] public partial bool InksWatercolorsIsWatercolorMode { get; set; }
    [ObservableProperty] public partial string? SelectedInkColor { get; set; }
    [ObservableProperty] public partial string DetailsInkSearchText { get; set; } = string.Empty;
    public InkSelection DetailsInks { get; } = new();

    public IEnumerable<InkColorChip> FilteredDetailsInkChips =>
        string.IsNullOrWhiteSpace(DetailsInkSearchText)
            ? DetailsInks.Chips
            : DetailsInks.Chips.Where(c => c.Color.Contains(DetailsInkSearchText.Trim(), StringComparison.OrdinalIgnoreCase));
    partial void OnDetailsInkSearchTextChanged(string value) => OnPropertyChanged(nameof(FilteredDetailsInkChips));

    [RelayCommand] private void SetInksMode() => InksWatercolorsIsWatercolorMode = false;
    [RelayCommand] private void SetWatercolorsMode() => InksWatercolorsIsWatercolorMode = true;

    [RelayCommand]
    private void SelectInkColor(string color)
    {
        SelectedInkColor = color;
        WatercolorsPicker.SelectedItem = null;
        InksWatercolorsIsOpen = false;
        NotifyDetailPreview();
    }

    [RelayCommand]
    private void SelectWatercolor(WizardItemOption item)
    {
        WatercolorsPicker.SelectedItem = item;
        SelectedInkColor = null;
        InksWatercolorsIsOpen = false;
        NotifyDetailPreview();
    }

    // Follow-up state
    public InkSelection StampInks { get; } = new();
    public WizardItemPicker StampEmbossingPowderPicker { get; } = new() { PlaceholderText = "Embossing Powder" };
    public bool StampHasDieCombo => StampsPicker.SelectedItem?.Subtype?.Contains("Die Combo", StringComparison.OrdinalIgnoreCase) == true;
    [ObservableProperty] public partial bool StampUsedAsCombo { get; set; }
    [ObservableProperty] public partial string StampComboLayers { get; set; } = "1";

    [ObservableProperty] public partial bool DieIsMultiLayer { get; set; }
    [ObservableProperty] public partial string DieLayers { get; set; } = "1";

    public InkSelection EmbellEmbossingInks { get; } = new();
    public WizardItemPicker EmbellEmbossingStampPicker { get; } = new() { PlaceholderText = "Stamp embossed with this powder" };
    public bool ShowEmbellEmbossingFollowups =>
        EmbellishmentsPicker.SelectedItem?.Subtype?.Contains("Embossing Powder", StringComparison.OrdinalIgnoreCase) == true;

    [ObservableProperty] public partial string StackletDieNumber { get; set; } = "1";
    [ObservableProperty] public partial string StackletLayers { get; set; } = "1";

    // Stencil per-layer stepper
    public ObservableCollection<WizardStencilLayerEntry> DetailStencilLayerEntries { get; } = new();
    [ObservableProperty] public partial int DetailStencilLayerIndex { get; set; }
    public WizardStencilLayerEntry? DetailStencilLayerEntry =>
        DetailStencilLayerIndex >= 0 && DetailStencilLayerIndex < DetailStencilLayerEntries.Count
            ? DetailStencilLayerEntries[DetailStencilLayerIndex] : null;
    public string DetailStencilLayerHeader => $"Layer {DetailStencilLayerIndex + 1} of {DetailStencilLayerEntries.Count}";
    public bool HasPreviousDetailStencilLayer => DetailStencilLayerIndex > 0;
    public bool HasNextDetailStencilLayer => DetailStencilLayerIndex < DetailStencilLayerEntries.Count - 1;
    public bool ShowDetailStencilLayers => StencilsPicker.SelectedItem != null && DetailStencilLayerEntries.Count > 0;

    partial void OnDetailStencilLayerIndexChanged(int value)
    {
        OnPropertyChanged(nameof(DetailStencilLayerEntry));
        OnPropertyChanged(nameof(DetailStencilLayerHeader));
        OnPropertyChanged(nameof(HasPreviousDetailStencilLayer));
        OnPropertyChanged(nameof(HasNextDetailStencilLayer));
    }

    [RelayCommand] private void PreviousDetailStencilLayer() { if (HasPreviousDetailStencilLayer) DetailStencilLayerIndex--; }
    [RelayCommand] private void NextDetailStencilLayer() { if (HasNextDetailStencilLayer) DetailStencilLayerIndex++; }

    // Foil follow-ups
    [ObservableProperty] public partial string FoilApplicationMethod { get; set; } = "GlitterGrab";
    public bool IsFoilGlitterGrabSelected => FoilApplicationMethod == "GlitterGrab";
    public bool IsFoilTonerSelected => FoilApplicationMethod == "Toner";
    partial void OnFoilApplicationMethodChanged(string value)
    {
        OnPropertyChanged(nameof(IsFoilGlitterGrabSelected));
        OnPropertyChanged(nameof(IsFoilTonerSelected));
    }

    [RelayCommand]
    private void SetFoilMethod(string method) => FoilApplicationMethod = method;

    public WizardItemPicker FoilStencilPicker { get; } = new() { PlaceholderText = "Stencil" };
    public InkSelection FoilStencilInks { get; } = new();
    [ObservableProperty] public partial bool FoilStencilUsedGlitter { get; set; }
    [ObservableProperty] public partial bool FoilStencilUsedHappyMedium { get; set; }
    [ObservableProperty] public partial bool FoilStencilUsedAstroPaste { get; set; }
    [ObservableProperty] public partial string FoilStencilGlitterLayers { get; set; } = string.Empty;
    [ObservableProperty] public partial string FoilStencilHappyMediumLayers { get; set; } = string.Empty;
    [ObservableProperty] public partial string FoilStencilAstroPasteLayers { get; set; } = string.Empty;
    public WizardItemPicker FoilStencilGlitterPicker { get; } = new() { PlaceholderText = "Which glitter?", IsMultiSelect = true };
    public WizardItemPicker FoilStencilHappyMediumPicker { get; } = new() { PlaceholderText = "Which happy medium?", IsMultiSelect = true };
    public WizardItemPicker FoilStencilAstroPastePicker { get; } = new() { PlaceholderText = "Which astro paste?", IsMultiSelect = true };
    [ObservableProperty] public partial string FoilTonerText { get; set; } = string.Empty;
    [ObservableProperty] public partial string? FoilTonerFont { get; set; }
    [ObservableProperty] public partial string FoilTonerCustomFont { get; set; } = string.Empty;
    public List<string> StandardFonts { get; } = new()
        { "Arial", "Times New Roman", "Georgia", "Courier New", "Comic Sans MS", "Brush Script MT", "Garamond", "Verdana" };

    // Follow-up gates
    public bool ShowStampFollowups => StampsPicker.SelectedItem != null;
    public bool ShowDieFollowups => DiesPicker.SelectedItem != null;
    public bool ShowStackletFollowups => StackletsPicker.SelectedItem != null;
    public bool ShowFoilFollowups => FoilsPicker.SelectedItem != null;
    public bool ShowAnyFollowups =>
        ShowStampFollowups || ShowDieFollowups || ShowStackletFollowups || ShowFoilFollowups ||
        ShowDetailStencilLayers || ShowEmbellEmbossingFollowups;

    public bool HasCurrentDetailPreview => HasAnyDetailSelection();
    public string CurrentDetailPreview
    {
        get
        {
            var parts = new List<string>();
            if (StampsPicker.SelectedItem is { } st) parts.Add(st.Name);
            if (DiesPicker.SelectedItem is { } di) parts.Add(di.Name);
            if (StencilsPicker.SelectedItem is { } sn) parts.Add(sn.Name);
            if (EmbellishmentsPicker.SelectedItem is { } em) parts.Add(em.Name);
            if (StackletsPicker.SelectedItem is { } sk) parts.Add(sk.Name);
            if (EmbossingFoldersPicker.SelectedItem is { } ef) parts.Add(ef.Name);
            if (FoilsPicker.SelectedItem is { } fo) parts.Add($"Foil: {fo.Name}");
            if (OloMarkersPicker.SelectedItems.Count > 0) parts.Add(string.Join(", ", OloMarkersPicker.SelectedItems.Select(o => o.Name)));
            if (WatercolorsPicker.SelectedItem is { } wc) parts.Add(wc.Name);
            if (DetailsInks.HasSelection) parts.Add($"Ink: {DetailsInks.DisplaySummary}");
            else if (SelectedInkColor is { } ic) parts.Add($"Ink: {ic}");
            return string.Join("   •   ", parts);
        }
    }

    private void NotifyDetailPreview()
    {
        OnPropertyChanged(nameof(ShowStampFollowups));
        OnPropertyChanged(nameof(ShowDieFollowups));
        OnPropertyChanged(nameof(ShowStackletFollowups));
        OnPropertyChanged(nameof(ShowFoilFollowups));
        OnPropertyChanged(nameof(ShowDetailStencilLayers));
        OnPropertyChanged(nameof(ShowEmbellEmbossingFollowups));
        OnPropertyChanged(nameof(ShowAnyFollowups));
        OnPropertyChanged(nameof(StampHasDieCombo));
        OnPropertyChanged(nameof(HasCurrentDetailPreview));
        OnPropertyChanged(nameof(CurrentDetailPreview));
        OnPropertyChanged(nameof(HasHubEmbellishmentPick));
        OnPropertyChanged(nameof(CurrentHubEmbellishmentPreview));
    }

    private bool HasAnyDetailSelection() =>
        StampsPicker.SelectedItem != null || DiesPicker.SelectedItem != null ||
        StencilsPicker.SelectedItem != null || EmbellishmentsPicker.SelectedItem != null ||
        StackletsPicker.SelectedItem != null || EmbossingFoldersPicker.SelectedItem != null ||
        FoilsPicker.SelectedItem != null || OloMarkersPicker.SelectedItems.Count > 0 ||
        WatercolorsPicker.SelectedItem != null || SelectedInkColor != null || DetailsInks.HasSelection;

    private void ClearDetailSelections()
    {
        StampsPicker.SelectedItem = null;
        DiesPicker.SelectedItem = null;
        StencilsPicker.SelectedItem = null;
        EmbellishmentsPicker.SelectedItem = null;
        StackletsPicker.SelectedItem = null;
        EmbossingFoldersPicker.SelectedItem = null;
        FoilsPicker.SelectedItem = null;
        OloMarkersPicker.SelectedItems.Clear();
        WatercolorsPicker.SelectedItem = null;
        SelectedInkColor = null;
        DetailsInks.Clear();
        StampInks.Clear();
        StampEmbossingPowderPicker.SelectedItem = null;
        StampUsedAsCombo = false; StampComboLayers = "1";
        DieIsMultiLayer = false; DieLayers = "1";
        EmbellEmbossingInks.Clear();
        EmbellEmbossingStampPicker.SelectedItem = null;
        StackletDieNumber = "1"; StackletLayers = "1";
        DetailStencilLayerEntries.Clear();
        DetailStencilLayerIndex = 0;
        FoilApplicationMethod = "GlitterGrab";
        FoilStencilPicker.SelectedItem = null;
        FoilStencilInks.Clear();
        FoilStencilUsedGlitter = FoilStencilUsedHappyMedium = FoilStencilUsedAstroPaste = false;
        FoilStencilGlitterLayers = FoilStencilHappyMediumLayers = FoilStencilAstroPasteLayers = string.Empty;
        FoilStencilGlitterPicker.SelectedItems.Clear();
        FoilStencilHappyMediumPicker.SelectedItems.Clear();
        FoilStencilAstroPastePicker.SelectedItems.Clear();
        FoilTonerText = string.Empty; FoilTonerFont = null; FoilTonerCustomFont = string.Empty;
        DetailsInkSearchText = string.Empty;
        NotifyDetailPreview();
    }

    private void CaptureCurrentDetailEntry()
    {
        var e = new WizardDetailEntry
        {
            Stamp = StampsPicker.SelectedItem,
            StampInkColors = StampInks.Ordered.ToList(),
            StampWasEmbossed = StampInks.IsEmbossed,
            StampEmbossingPowder = StampEmbossingPowderPicker.SelectedItem,
            StampUsedAsCombo = StampHasDieCombo && StampUsedAsCombo,
            StampComboLayers = int.TryParse(StampComboLayers, out var scl) ? scl : 1,
            Die = DiesPicker.SelectedItem,
            DieIsMultiLayer = DieIsMultiLayer,
            DieLayers = int.TryParse(DieLayers, out var dl) ? dl : 1,
            Embellishment = EmbellishmentsPicker.SelectedItem,
            EmbellEmbossingInkColors = EmbellEmbossingInks.Ordered.ToList(),
            EmbellEmbossingStamp = EmbellEmbossingStampPicker.SelectedItem,
            Stacklet = StackletsPicker.SelectedItem,
            StackletDieNumber = int.TryParse(StackletDieNumber, out var sdn) ? sdn : 1,
            StackletLayers = int.TryParse(StackletLayers, out var sl) ? sl : 1,
            EmbossingFolder = EmbossingFoldersPicker.SelectedItem,
            OloMarkers = OloMarkersPicker.SelectedItems.ToList(),
            Watercolor = WatercolorsPicker.SelectedItem,
            InkColor = DetailsInks.HasSelection ? string.Join(", ", DetailsInks.Ordered) : SelectedInkColor,
            Stencil = StencilsPicker.SelectedItem,
        };

        // Stencil layer capture
        if (e.Stencil != null && DetailStencilLayerEntries.Count > 0)
        {
            e.StencilLayerEntries = DetailStencilLayerEntries.Select(le => new WizardStencilLayer
            {
                LayerNumber = le.LayerNumber,
                InkColors = le.Inks.Ordered.ToList(),
                UsedGlitter = le.UsedGlitter,
                UsedHappyMedium = le.UsedHappyMedium,
                UsedAstroPaste = le.UsedAstroPaste,
                GlitterItems = le.GlitterPicker.SelectedItems.ToList(),
                HappyMediumItems = le.HappyMediumPicker.SelectedItems.ToList(),
                AstroPasteItems = le.AstroPastePicker.SelectedItems.ToList(),
            }).ToList();
            e.StencilInkColors = e.StencilLayerEntries.SelectMany(x => x.InkColors).Distinct().ToList();
            e.StencilUsedGlitter = e.StencilLayerEntries.Any(x => x.UsedGlitter);
            e.StencilUsedHappyMedium = e.StencilLayerEntries.Any(x => x.UsedHappyMedium);
            e.StencilUsedAstroPaste = e.StencilLayerEntries.Any(x => x.UsedAstroPaste);
        }

        // Foil capture
        if (e.Foil == null && FoilsPicker.SelectedItem != null) e.Foil = FoilsPicker.SelectedItem;
        if (e.Foil != null)
        {
            e.FoilApplicationMethod = FoilApplicationMethod;
            if (IsFoilGlitterGrabSelected)
            {
                e.FoilStencil = FoilStencilPicker.SelectedItem;
                e.FoilStencilInkColors = FoilStencilInks.Ordered.ToList();
                e.FoilStencilUsedGlitter = FoilStencilUsedGlitter;
                e.FoilStencilUsedHappyMedium = FoilStencilUsedHappyMedium;
                e.FoilStencilUsedAstroPaste = FoilStencilUsedAstroPaste;
                e.FoilStencilGlitterLayers = FoilStencilGlitterLayers;
                e.FoilStencilHappyMediumLayers = FoilStencilHappyMediumLayers;
                e.FoilStencilAstroPasteLayers = FoilStencilAstroPasteLayers;
                e.FoilStencilGlitterItems = FoilStencilGlitterPicker.SelectedItems.ToList();
                e.FoilStencilHappyMediumItems = FoilStencilHappyMediumPicker.SelectedItems.ToList();
                e.FoilStencilAstroPasteItems = FoilStencilAstroPastePicker.SelectedItems.ToList();
            }
            else
            {
                e.FoilTonerText = FoilTonerText;
                e.FoilTonerFont = string.IsNullOrWhiteSpace(FoilTonerCustomFont) ? FoilTonerFont : FoilTonerCustomFont;
            }
        }

        switch (DetailsReturnTarget)
        {
            case "CardBase": CardBaseAddedDetails.Add(e); CardBaseDetailsSaved = true; break;
            case "BackgroundMat" or "AdditionalMat" or "FocalMat":
                CurrentMat.AddedDetails.Add(e); BgPieceDetailsSaved = true; break;
            case "Sentiment": _pendingSentimentDetails.Add(e); SentimentPieceDetailsSaved = true; break;
            case "InsideMisc": InsideMiscDetails.Add(e); InsideDetailsSaved = true; break;
            default: CardBaseAddedDetails.Add(e); break;
        }
        OnPropertyChanged(nameof(AddedDetailsForCurrentTarget));
    }

    [RelayCommand]
    private void SaveDetailsAndAddAnother()
    {
        if (HasAnyDetailSelection()) CaptureCurrentDetailEntry();
        ClearDetailSelections();
        UpdateSummaryLines();
    }

    [RelayCommand]
    private void SaveDetailsAndReturn()
    {
        if (HasAnyDetailSelection()) CaptureCurrentDetailEntry();
        ClearDetailSelections();
        UpdateSummaryLines();
        switch (DetailsReturnTarget)
        {
            case "CardBase": CurrentCardBaseStep = "Hub"; break;
            case "BackgroundMat" or "AdditionalMat" or "FocalMat": BgMatHubStep = "Hub"; break;
            case "Sentiment": SentimentSubStep = "Hub"; break;
            case "InsideMisc": CurrentSection = "Hub"; break;
            default: CurrentSection = "Hub"; break;
        }
    }

    [RelayCommand]
    private void BackFromDetails() => SaveDetailsAndReturnCommand.Execute(null);

    [RelayCommand]
    private void RemoveAddedDetail(WizardDetailEntry entry)
    {
        CardBaseAddedDetails.Remove(entry);
        CurrentMat.AddedDetails.Remove(entry);
        _pendingSentimentDetails.Remove(entry);
        InsideMiscDetails.Remove(entry);
        CardBaseDetailsSaved = CardBaseAddedDetails.Count > 0;
        BgPieceDetailsSaved = CurrentMat.AddedDetails.Count > 0;
        InsideDetailsSaved = InsideMiscDetails.Count > 0;
        OnPropertyChanged(nameof(AddedDetailsForCurrentTarget));
        UpdateSummaryLines();
    }

    [RelayCommand]
    private void NavToInsideDetails()
    {
        ClearDetailSelections();
        DetailsReturnTarget = "InsideMisc";
        CurrentSection = "InsideDetails";
    }

    // ═══ SENTIMENTS ═════════════════════════════════════════════════════

    [ObservableProperty] public partial string SentimentSearchQuery { get; set; } = string.Empty;
    [ObservableProperty] public partial bool SentimentFilterDies { get; set; }
    [ObservableProperty] public partial bool SentimentFilterStamps { get; set; }
    [ObservableProperty] public partial bool SentimentFilterFullSets { get; set; }
    [ObservableProperty] public partial bool SentimentFilterThemeSearch { get; set; }
    public ObservableCollection<WizardSentimentResult> SentimentResults { get; } = new();

    [ObservableProperty] public partial WizardSentimentResult? CurrentSentimentResult { get; set; }
    [ObservableProperty] public partial bool IsConfiguringCurrentSentiment { get; set; }

    public ObservableCollection<WizardConfiguredSentiment> ConfiguredSentiments { get; } = new();
    public ObservableCollection<WizardConfiguredSentiment> ConfiguredInsideSentiments { get; } = new(); // legacy
    private readonly List<WizardConfiguredSentimentPart> _currentSentimentParts = new();
    private readonly List<string> _sentimentOtherNotes = new();

    public InkSelection SentimentInks { get; } = new();
    public WizardItemPicker SentimentStampEmbossingPowderPicker { get; } = new() { PlaceholderText = "Embossing Powder" };

    [ObservableProperty] public partial bool SentimentPieceCardstockSaved { get; set; }
    [ObservableProperty] public partial bool SentimentPieceDetailsSaved { get; set; }
    [ObservableProperty] public partial bool SentimentPieceAdhesivesSaved { get; set; }

    public WizardItemPicker SentimentCardstockPicker { get; } = new() { PlaceholderText = "Cardstock" };
    public WizardItemPicker SentimentFoilCardstockPicker { get; } = new() { PlaceholderText = "Foil Cardstock" };
    public WizardItemPicker SentimentGlitterCardstockPicker { get; } = new() { PlaceholderText = "Glitter Cardstock" };
    [ObservableProperty] public partial bool SentimentIsSelfBlended { get; set; }
    [ObservableProperty] public partial string SentimentSelfBlendDescription { get; set; } = string.Empty;
    public InkSelection SentimentBlendInks { get; } = new();
    private string? _sentimentCardstockColor;

    public WizardItemPicker SentimentGlueAdhesivePicker { get; } = new() { PlaceholderText = "Glue" };
    public WizardItemPicker SentimentFoamAdhesivePicker { get; } = new() { PlaceholderText = "Foam" };
    public WizardItemPicker SentimentTapeRunnerAdhesivePicker { get; } = new() { PlaceholderText = "Tape Runner" };
    private readonly List<string> _sentimentAdhesives = new();

    public bool HasCurrentSentimentAdhesivePick =>
        SentimentGlueAdhesivePicker.SelectedItem != null || SentimentFoamAdhesivePicker.SelectedItem != null || SentimentTapeRunnerAdhesivePicker.SelectedItem != null;
    public string CurrentSentimentAdhesivePreview => string.Join("   •   ",
        new[] { SentimentGlueAdhesivePicker.SelectedItem?.Name, SentimentFoamAdhesivePicker.SelectedItem?.Name, SentimentTapeRunnerAdhesivePicker.SelectedItem?.Name }
        .Where(n => n != null));

    [RelayCommand]
    private void NavToSentiment()
    {
        SentimentSubStep = "Hub";
        CurrentSection = "Sentiment";
    }

    [RelayCommand]
    private async Task SearchSentiments()
    {
        SentimentResults.Clear();
        var q = SentimentSearchQuery.Trim();
        if (q.Length == 0) return;

        using var db = new InventoryDbContext();
        var query = db.Items.AsNoTracking().AsQueryable();

        if (SentimentFilterThemeSearch)
            query = query.Where(i => i.Subtype != null && EF.Functions.Like(i.Subtype, $"%{q}%"));
        else
            query = query.Where(i => i.Sentiments != null && EF.Functions.Like(i.Sentiments, $"%{q}%"));

        if (SentimentFilterDies) query = query.Where(i => i.Type == "Dies");
        else if (SentimentFilterStamps) query = query.Where(i => i.Type == "Stamps");
        if (SentimentFilterFullSets) query = query.Where(i => i.Subtype != null && i.Subtype.Contains("Full Set"));

        var rows = await query.OrderBy(i => i.Name).Take(60)
            .Select(i => new { i.Id, i.Name, i.Sentiments, i.ImageUrl, i.Type }).ToListAsync();

        foreach (var r in rows)
        {
            SentimentResults.Add(new WizardSentimentResult
            {
                ItemId = r.Id,
                ItemName = r.Name,
                SentimentPreview = r.Sentiments?.Length > 160 ? r.Sentiments[..160] + "…" : r.Sentiments,
                ThumbnailBase64 = r.ImageUrl,
            });
        }
    }

    [RelayCommand]
    private void SelectSentimentResult(WizardSentimentResult result)
    {
        CurrentSentimentResult = result;
        IsConfiguringCurrentSentiment = true;
    }

    [RelayCommand]
    private void CancelSentimentConfig()
    {
        CurrentSentimentResult = null;
        IsConfiguringCurrentSentiment = false;
    }

    [RelayCommand] private void NavSentimentToCardstock() => SentimentSubStep = "Cardstock";
    [RelayCommand]
    private void NavSentimentToDetails()
    {
        ClearDetailSelections();
        DetailsReturnTarget = "Sentiment";
        SentimentSubStep = "Details";
    }
    [RelayCommand] private void NavSentimentToAdhesives() => SentimentSubStep = "Adhesives";
    [RelayCommand] private void BackToSentimentHub() => SentimentSubStep = "Hub";

    [RelayCommand]
    private void SaveSentimentCardstock()
    {
        var pick = SentimentCardstockPicker.SelectedItem
                   ?? SentimentFoilCardstockPicker.SelectedItem
                   ?? SentimentGlitterCardstockPicker.SelectedItem;
        _sentimentCardstockColor = pick?.Name;
        SentimentPieceCardstockSaved = pick != null || SentimentIsSelfBlended;
        SentimentSubStep = "Hub";
    }

    [RelayCommand]
    private void SaveSentimentAdhesives()
    {
        foreach (var p in new[] { SentimentGlueAdhesivePicker, SentimentFoamAdhesivePicker, SentimentTapeRunnerAdhesivePicker })
        {
            if (p.SelectedItem is { } it && !_sentimentAdhesives.Contains(it.Name))
                _sentimentAdhesives.Add(it.Name);
            p.SelectedItem = null;
        }
        SentimentPieceAdhesivesSaved = _sentimentAdhesives.Count > 0;
        SentimentSubStep = "Hub";
    }

    private void CaptureCurrentSentimentPart()
    {
        if (CurrentSentimentResult is not { } cur) return;
        var part = new WizardConfiguredSentimentPart
        {
            ItemId = cur.ItemId,
            ItemName = cur.ItemName,
            ThumbnailBase64 = cur.ThumbnailBase64,
            IsStampType = true,
            CardstockColor = _sentimentCardstockColor,
            IsSelfBlended = SentimentIsSelfBlended,
            SelfBlendDescription = SentimentSelfBlendDescription,
            IsEmbossed = SentimentInks.IsEmbossed,
            EmbossingPowderName = SentimentStampEmbossingPowderPicker.SelectedItem?.Name,
            EmbossingPowderItemId = SentimentStampEmbossingPowderPicker.SelectedItem?.Id,
        };
        part.BlendInkColors.AddRange(SentimentBlendInks.Ordered);
        part.StampInkColors.AddRange(SentimentInks.Ordered);
        part.Adhesives.AddRange(_sentimentAdhesives);
        foreach (var d in _pendingSentimentDetails) part.AddedDetails.Add(d);
        _currentSentimentParts.Add(part);

        // reset per-part state
        _pendingSentimentDetails.Clear();
        _sentimentAdhesives.Clear();
        _sentimentCardstockColor = null;
        SentimentInks.Clear();
        SentimentBlendInks.Clear();
        SentimentIsSelfBlended = false;
        SentimentSelfBlendDescription = string.Empty;
        SentimentStampEmbossingPowderPicker.SelectedItem = null;
        SentimentPieceCardstockSaved = SentimentPieceDetailsSaved = SentimentPieceAdhesivesSaved = false;
    }

    [RelayCommand]
    private void AddSentimentPiece()
    {
        CaptureCurrentSentimentPart();
        CurrentSentimentResult = null;
        IsConfiguringCurrentSentiment = false;
    }

    [RelayCommand]
    private void FinishSentiment()
    {
        CaptureCurrentSentimentPart();
        if (_currentSentimentParts.Count > 0)
        {
            var s = new WizardConfiguredSentiment { IsInside = IsInsideMode };
            s.Parts.AddRange(_currentSentimentParts);
            ConfiguredSentiments.Add(s);
            _currentSentimentParts.Clear();
            SentimentSaved = true;
        }
        CurrentSentimentResult = null;
        IsConfiguringCurrentSentiment = false;
        SentimentResults.Clear();
        SentimentSearchQuery = string.Empty;
        NotifyInsideDoneIndicators();
        UpdateSummaryLines();
        CurrentSection = "Hub";
    }

    // ═══ EMBELLISHMENTS ════════════════════════════════════════════════════

    public ObservableCollection<WizardEmbellishment> AddedEmbellishments { get; } = new();
    public ObservableCollection<WizardEmbellishment> InsideAddedEmbellishments { get; } = new(); // legacy

    public bool HasHubEmbellishmentPick => EmbellishmentsPicker.SelectedItem != null;
    public string CurrentHubEmbellishmentPreview => EmbellishmentsPicker.SelectedItem?.Name ?? "";

    [RelayCommand]
    private void NavToEmbellishments() => CurrentSection = "Embellishments";

    private void CaptureHubEmbellishment()
    {
        if (EmbellishmentsPicker.SelectedItem is not { } pick) return;
        var emb = new WizardEmbellishment
        {
            ItemId = pick.Id,
            ItemName = pick.Name,
            Subtype = pick.Subtype,
            IsInside = IsInsideMode,
        };
        if (ShowEmbellEmbossingFollowups)
        {
            emb.StampItemId = EmbellEmbossingStampPicker.SelectedItem?.Id;
            emb.StampItemName = EmbellEmbossingStampPicker.SelectedItem?.Name;
            emb.InkColors = EmbellEmbossingInks.Ordered.ToList();
        }
        AddedEmbellishments.Add(emb);
        EmbellishmentsSaved = true;
        EmbellishmentsPicker.SelectedItem = null;
        EmbellEmbossingStampPicker.SelectedItem = null;
        EmbellEmbossingInks.Clear();
        NotifyInsideDoneIndicators();
        NotifyDetailPreview();
    }

    [RelayCommand]
    private void SaveHubEmbellishmentAndAddAnother()
    {
        CaptureHubEmbellishment();
        UpdateSummaryLines();
    }

    [RelayCommand]
    private void SaveHubEmbellishmentAndReturn()
    {
        CaptureHubEmbellishment();
        UpdateSummaryLines();
        CurrentSection = "Hub";
    }

    [RelayCommand]
    private void RemoveEmbellishment(WizardEmbellishment emb)
    {
        AddedEmbellishments.Remove(emb);
        EmbellishmentsSaved = AddedEmbellishments.Count > 0;
        NotifyInsideDoneIndicators();
        UpdateSummaryLines();
    }

    // ═══ INSIDE CARDSTOCK / FOCAL (legacy holders) ═════════════════════════

    public WizardItemPicker InsideLinerCardstockPicker { get; } = new() { PlaceholderText = "Cardstock for inside" };
    [ObservableProperty] public partial WizardItemOption? SelectedInsideLinerCardstockItem { get; set; }
    [ObservableProperty] public partial string? SelectedInsideLinerCardstockColor { get; set; }

    public ObservableCollection<WizardBgMat> InsideBgMats { get; } = new();          // legacy
    public ObservableCollection<WizardBgMat> InsideAdditionalMats { get; } = new();  // legacy
    public WizardFocalSection InsideFocal { get; set; } = new();
    [ObservableProperty] public partial bool HasInsideFocalMat { get; set; }
    public bool HasInside =>
        BgMats.Any(g => g.IsInside) || AdditionalMats.Any(g => g.IsInside) || FocalMatGroups.Any(g => g.IsInside) ||
        InsideBgMats.Count > 0 || InsideAdditionalMats.Count > 0 || HasInsideFocalMat ||
        ConfiguredSentiments.Any(s => s.IsInside) || AddedEmbellishments.Any(e => e.IsInside) ||
        SelectedInsideLinerCardstockItem != null || InsideMiscDetails.Count > 0;

    [RelayCommand]
    private void NavToInsideCardstock() => CurrentSection = "InsideCardstock";

    [RelayCommand]
    private void SaveInsideCardstockAndBackToHub()
    {
        SelectedInsideLinerCardstockItem = InsideLinerCardstockPicker.SelectedItem;
        SelectedInsideLinerCardstockColor = SelectedInsideLinerCardstockItem?.Name;
        InsideCardstockSaved = SelectedInsideLinerCardstockItem != null;
        CurrentSection = "Hub";
        UpdateSummaryLines();
    }

    // ═══ ENVELOPE / STORAGE BAG ════════════════════════════════════════════

    public WizardItemPicker EnvelopesPicker { get; } = new() { PlaceholderText = "Envelopes" };
    [ObservableProperty] public partial WizardItemOption? SelectedEnvelopeItem { get; set; }
    [ObservableProperty] public partial WizardItemOption? SelectedStorageBagItem { get; set; }
    public ObservableCollection<WizardItemOption> StorageBagItems { get; } = new();

    // ═══ Picker overlay (MAUI stand-in for the desktop's dropdown popups) ══
    // One shared overlay: any pill button opens its picker here; selecting an
    // item sets picker.SelectedItem (single) or toggles membership (multi).

    [ObservableProperty] public partial WizardItemPicker? ActivePicker { get; set; }
    public bool IsPickerOpen => ActivePicker != null;
    partial void OnActivePickerChanged(WizardItemPicker? value) => OnPropertyChanged(nameof(IsPickerOpen));

    [RelayCommand] private void OpenPicker(WizardItemPicker picker) => ActivePicker = picker;
    [RelayCommand] private void ClosePicker() => ActivePicker = null;

    [RelayCommand]
    private void PickOverlayItem(WizardItemOption item)
    {
        if (ActivePicker is not { } p) return;
        if (p.IsMultiSelect)
        {
            var existing = p.SelectedItems.FirstOrDefault(x => x.Id == item.Id);
            if (existing != null) p.SelectedItems.Remove(existing);
            else p.SelectedItems.Add(item);
            // stays open; Done closes
        }
        else
        {
            p.SelectedItem = item;
            ActivePicker = null;
        }
        NotifyDetailPreview();
    }

    [RelayCommand]
    private void ClearOverlayPicker()
    {
        if (ActivePicker is not { } p) return;
        p.SelectedItem = null;
        p.SelectedItems.Clear();
        ActivePicker = null;
        NotifyDetailPreview();
    }

    // ═══ Name→id lookup maps (used by snapshot restore + assemble) ═════════
    private readonly Dictionary<string, int> _inkItemIdByColor = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _cardstockItemIdByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _foilCardstockIdByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _glitterCardstockIdByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _adhesiveIdByName = new(StringComparer.OrdinalIgnoreCase);

    private int InkItemIdFor(string color) => _inkItemIdByColor.TryGetValue(color, out var id) ? id : 0;

    // ═══ INITIALIZATION ═════════════════════════════════════════════════════

    public async Task InitializeAsync(int projectId, string projectName, string? projectImage, string? existingSnapshot)
    {
        ProjectName = projectName;
        ProjectImage = projectImage;

        async Task<List<WizardItemOption>> L(string label) => await _labels.GetItemsForLabelAsync(label);

        var stamps = await L("Stamps");
        var dies = await L("Dies");
        var stencils = await L("Stencils");
        var embellishments = await L("Embellishments");
        var stacklets = await L("Stacklets");
        var embossingFolders = await L("Embossing Folders");
        var foils = await L("Foils");
        var oloMarkers = await L("OLO Markers");
        var watercolors = await L("Watercolor");
        var cardstock = await L("Cardstock");
        var foilCardstock = await L("Foil Cardstock");
        var glitterCardstock = await L("Glitter Cardstock");
        var insiderCardstock = await L("Insider Cardstock");
        var foilItCardstock = await L("Foil-It Cardstock");
        var framesDies = await L("Frames Die");
        var plannedOut = await L("All Planned Out");
        var envelopes = await L("Envelopes");
        var storageBags = await L("Storage Bags");
        var glue = await L("Glue Adhesive");
        var foam = await L("Foam Adhesive");
        var tape = await L("Tape Runner Adhesive");
        var miniCube = await L("Mini Cube Inks");
        var fullPad = await L("Full Pad Inks");
        var powder = await L("Embossing Powder");
        var happyMedium = await L("Happy Medium");
        var astroPaste = await L("Astro Paste");
        var glitterItems = await L("Glitter");

        StampsPicker.Load(stamps);
        DiesPicker.Load(dies);
        StencilsPicker.Load(stencils);
        EmbellishmentsPicker.Load(embellishments);
        StackletsPicker.Load(stacklets);
        EmbossingFoldersPicker.Load(embossingFolders);
        FoilsPicker.Load(foils);
        OloMarkersPicker.Load(oloMarkers);
        WatercolorsPicker.Load(watercolors);
        BaseRegularCardstockPicker.Load(cardstock);
        BaseFoilCardstockPicker.Load(foilCardstock);
        BaseGlitterCardstockPicker.Load(glitterCardstock);
        BgPieceCardstockPicker.Load(cardstock);
        BgPieceFoilCardstockPicker.Load(foilCardstock);
        BgPieceGlitterCardstockPicker.Load(glitterCardstock);
        SentimentCardstockPicker.Load(cardstock);
        SentimentFoilCardstockPicker.Load(foilCardstock);
        SentimentGlitterCardstockPicker.Load(glitterCardstock);
        InsideLinerCardstockPicker.Load(cardstock);
        BgCutStackletsPicker.Load(stacklets);
        BgCutPlannedOutPicker.Load(plannedOut);
        BgCutFramesPicker.Load(framesDies);
        BgCutInsiderPicker.Load(insiderCardstock);
        BgCutFoilItPicker.Load(foilItCardstock);
        BgCutFoilsPicker.Load(foils);
        GlueAdhesivePicker.Load(glue);
        FoamAdhesivePicker.Load(foam);
        TapeRunnerAdhesivePicker.Load(tape);
        BgPieceGlueAdhesivePicker.Load(glue);
        BgPieceFoamAdhesivePicker.Load(foam);
        BgPieceTapeRunnerAdhesivePicker.Load(tape);
        SentimentGlueAdhesivePicker.Load(glue);
        SentimentFoamAdhesivePicker.Load(foam);
        SentimentTapeRunnerAdhesivePicker.Load(tape);
        StampEmbossingPowderPicker.Load(powder);
        SentimentStampEmbossingPowderPicker.Load(powder);
        EmbellEmbossingStampPicker.Load(stamps);
        FoilStencilPicker.Load(stencils);
        FoilStencilGlitterPicker.Load(glitterItems);
        FoilStencilHappyMediumPicker.Load(happyMedium);
        FoilStencilAstroPastePicker.Load(astroPaste);
        EnvelopesPicker.Load(envelopes);
        foreach (var b in storageBags) StorageBagItems.Add(b);

        foreach (var c in cardstock) BaseCardstockRegularItems.Add(c);
        foreach (var c in foilCardstock) BaseCardstockFoilItems.Add(c);
        foreach (var c in glitterCardstock) BaseCardstockGlitterItems.Add(c);

        // Ink color options: Mini Cube preferred over Full Pad for the item-id lookup.
        foreach (var i in fullPad) _inkItemIdByColor[i.Name] = i.Id;
        foreach (var i in miniCube) _inkItemIdByColor[i.Name] = i.Id;
        var inkColors = miniCube.Select(i => i.Name)
            .Concat(fullPad.Select(i => i.Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();

        foreach (var ink in new[] { BaseBlendInks, StampInks, EmbellEmbossingInks, DetailsInks, BgPieceBlendInks, SentimentInks, SentimentBlendInks, FoilStencilInks })
            ink.SetColors(inkColors, InkItemIdFor);
        DetailsInks.SetWatercolors(watercolors);

        foreach (var c in cardstock) _cardstockItemIdByName[c.Name] = c.Id;
        foreach (var c in foilCardstock) _foilCardstockIdByName[c.Name] = c.Id;
        foreach (var c in glitterCardstock) _glitterCardstockIdByName[c.Name] = c.Id;
        foreach (var a in glue.Concat(foam).Concat(tape)) _adhesiveIdByName[a.Name] = a.Id;

        WireBgCutPickerEchoes();
        WirePickerNotifications(inkColors);

        // Envelope echo (spec: selection mirrors + flips EnvelopeSaved)
        EnvelopesPicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WizardItemPicker.SelectedItem)) return;
            SelectedEnvelopeItem = EnvelopesPicker.SelectedItem;
            EnvelopeSaved = SelectedEnvelopeItem != null;
            UpdateSummaryLines();
        };

        // Stencil selection → build the per-layer stepper
        StencilsPicker.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WizardItemPicker.SelectedItem)) return;
            DetailStencilLayerEntries.Clear();
            if (StencilsPicker.SelectedItem is { } st)
            {
                int layers = Math.Max(1, st.StencilLayers ?? 1);
                for (int i = 1; i <= layers; i++)
                {
                    var entry = new WizardStencilLayerEntry { LayerNumber = i };
                    entry.Inks.SetColors(inkColors, InkItemIdFor);
                    entry.GlitterPicker.Load(glitterItems);
                    entry.HappyMediumPicker.Load(happyMedium);
                    entry.AstroPastePicker.Load(astroPaste);
                    DetailStencilLayerEntries.Add(entry);
                }
            }
            DetailStencilLayerIndex = 0;
            NotifyDetailPreview();
            OnPropertyChanged(nameof(DetailStencilLayerHeader));
            OnPropertyChanged(nameof(DetailStencilLayerEntry));
        };

        if (!string.IsNullOrWhiteSpace(existingSnapshot))
            LoadFromSnapshotJson(existingSnapshot);

        UpdateSummaryLines();
    }

    /// <summary>Refresh follow-up gates whenever any details picker changes.</summary>
    private void WirePickerNotifications(List<string> inkColors)
    {
        foreach (var p in new[]
                 {
                     StampsPicker, DiesPicker, EmbellishmentsPicker, StackletsPicker,
                     EmbossingFoldersPicker, FoilsPicker, OloMarkersPicker, WatercolorsPicker,
                 })
        {
            p.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(WizardItemPicker.SelectedItem) or nameof(WizardItemPicker.DisplayLabel))
                    NotifyDetailPreview();
            };
        }
    }
}
