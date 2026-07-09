using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MyCraftyStash.Models;
using MyCraftyStash.Services;
namespace MyCraftyStash.ViewModels
{
    // ── Wizard helper types ────────────────────────────────────────────────────

    public class WizardStencilLayer
    {
        public int LayerNumber { get; set; }
        public List<string> InkColors { get; set; } = new();
        // Per-layer special-media flags. Populated by the new stencil layer
        // stepper in WizardDetailEntry so the summary + used-item logs can
        // attribute Glitter / Happy Medium / Astro Paste to the exact layer
        // they were applied on.
        public bool UsedGlitter { get; set; }
        public bool UsedHappyMedium { get; set; }
        public bool UsedAstroPaste { get; set; }
        // The specific glitter / Happy Medium / Astro Paste inventory items the
        // user picked for this layer. Multi-select per layer; flow into the
        // used-items log via WizardDetailEntry.GetItemIds().
        public List<WizardItemOption> GlitterItems { get; set; } = new();
        public List<WizardItemOption> HappyMediumItems { get; set; } = new();
        public List<WizardItemOption> AstroPasteItems { get; set; } = new();
        public string DisplaySummary
        {
            get
            {
                var bits = new List<string>();
                if (InkColors.Count > 0) bits.Add(string.Join(", ", InkColors));
                if (UsedGlitter)
                {
                    var label = "Glitter";
                    if (GlitterItems.Count > 0) label += $" ({string.Join(", ", GlitterItems.Select(i => i.Name))})";
                    bits.Add(label);
                }
                if (UsedHappyMedium)
                {
                    var label = "Happy Medium";
                    if (HappyMediumItems.Count > 0) label += $" ({string.Join(", ", HappyMediumItems.Select(i => i.Name))})";
                    bits.Add(label);
                }
                if (UsedAstroPaste)
                {
                    var label = "Astro Paste";
                    if (AstroPasteItems.Count > 0) label += $" ({string.Join(", ", AstroPasteItems.Select(i => i.Name))})";
                    bits.Add(label);
                }
                return bits.Count == 0
                    ? $"Layer {LayerNumber}: (no entries)"
                    : $"Layer {LayerNumber}: {string.Join(" + ", bits)}";
            }
        }
    }

    public class WizardMatDecoration
    {
        public WizardItemOption Item { get; set; } = null!;
        public WizardItemOption? StampItem { get; set; }
        public List<string> StampInkColors { get; } = new();
        public List<string> EmbossingInkColors { get; } = new();
        public List<WizardStencilLayer> StencilInkLayers { get; } = new();

        public string DisplaySummary
        {
            get
            {
                var s = Item.Name;
                if (StampInkColors.Count > 0) s += $" [{string.Join(", ", StampInkColors)}]";
                if (StampItem != null) s += $" (stamp: {StampItem.Name})";
                if (EmbossingInkColors.Count > 0) s += $" [{string.Join(", ", EmbossingInkColors)}]";
                if (StencilInkLayers.Count > 0)
                    s += $" [{string.Join(" / ", StencilInkLayers.Select(l => string.Join("+", l.InkColors)))}]";
                return s;
            }
        }
    }

    public partial class WizardBgMat : ObservableObject
    {
        public static List<string> CuttingMethodOptions { get; } = new()
            { "Stacklets", "All Planned Out", "Frames", "Insider", "Foil-It", "Custom", "None" };

        public int Layer { get; set; }

        // Default to empty so the new How-Was-Cut sub-page's visibility-gated follow-ups
        // (Cut details panel) stay hidden until the user actually picks a method. The
        // legacy form, which had this defaulting to "Stacklets", is no longer the
        // primary path and renders fine with an empty initial value.
        [ObservableProperty] private string _cuttingMethod = string.Empty;

        // All Planned Out
        [ObservableProperty] private WizardItemOption? _plannedOutItem;

        // Frames (uses Dies items filtered to subtype "Frames")
        [ObservableProperty] private WizardItemOption? _framesItem;
        [ObservableProperty] private string _framesDieNumber = string.Empty;
        public bool HasFramesItem => FramesItem != null;

        // Stacklet
        [ObservableProperty] private WizardItemOption? _stackletItem;
        [ObservableProperty] private string _stackletDieNumber = string.Empty;
        public bool HasStackletItem => StackletItem != null;

        // ── How-Was-Cut follow-ups (new hub) ──────────────────────────────────
        // Generic per-piece die index (1 = largest in set) and layer count for whichever
        // method is active. Stacklets and Frames use both; All Planned Out uses only
        // CutLayers; Custom / Insider / Foil-It / None don't use either.
        [ObservableProperty] private int _cutDieIndex = 1;
        [ObservableProperty] private int _cutLayers   = 1;
        partial void OnCutDieIndexChanged(int v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnCutLayersChanged(int v)   => OnPropertyChanged(nameof(DisplaySummary));

        // Foil item used (only when CuttingMethod is Foil-It or Insider). Drawn from
        // type="Foils" inventory and stored separately from the primary cut item so the
        // user can pick e.g. "Insider X" + foil sheet "Y" together.
        [ObservableProperty] private WizardItemOption? _foilsItem;
        partial void OnFoilsItemChanged(WizardItemOption? v) => OnPropertyChanged(nameof(DisplaySummary));

        // Note: a "secondary cut item" is no longer stored separately — the user
        // picks the cut tool from the top-level Stacklets / Frames / All Planned Out
        // dropdowns alongside an Insider or Foil-It variant (mutual exclusion split
        // into "cut tools" and "cardstock variants" groups).

        // Insider
        [ObservableProperty] private WizardItemOption? _insiderItem;
        [ObservableProperty] private string? _insiderSentiment;

        // Foil-It
        [ObservableProperty] private WizardItemOption? _foilItItem;

        // Mat Decoration
        [ObservableProperty] private bool _hasDecoration;
        [ObservableProperty] private WizardItemOption? _decorationItem;
        [ObservableProperty] private WizardItemOption? _decorationStampItem;
        public List<WizardStencilLayer> StencilInkLayers { get; } = new();
        public List<string> StampInkColors { get; } = new();
        public List<string> EmbossingInkColors { get; } = new();
        public List<string> Adhesives { get; } = new();
        public ObservableCollection<WizardMatDecoration> Decorations { get; } = new();
        // Captured from the new Details sub-page (one entry per Save & Add Another).
        // Coexists with the legacy Decorations field — both are read by summary/build-step code.
        // Alternative considered: translate WizardDetailEntry → WizardMatDecoration on save
        // so there's only one storage shape; rejected because WizardDetailEntry carries
        // richer per-picker follow-up answers we'd lose in translation.
        public ObservableCollection<WizardDetailEntry> AddedDetails { get; } = new();

        // Cardstock color for this mat
        [ObservableProperty] private string? _selectedCardstockColor;
        [ObservableProperty] private string _otherCardstockText = string.Empty;

        // Self-blended cardstock
        [ObservableProperty] private bool _isSelfBlended;
        [ObservableProperty] private string _selfBlendDescription = string.Empty;
        public List<string> BlendInkColors { get; } = new();

        public string EffectiveCardstockColor =>
            SelectedCardstockColor == "Other" ? OtherCardstockText : SelectedCardstockColor ?? string.Empty;

        public bool ShowDecorationStampSection =>
            DecorationItem?.Subtype?.Contains("Embossing Powder", StringComparison.OrdinalIgnoreCase) ?? false;

        partial void OnIsSelfBlendedChanged(bool value) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnSelfBlendDescriptionChanged(string value) => OnPropertyChanged(nameof(DisplaySummary));

        partial void OnStackletItemChanged(WizardItemOption? value)
        {
            OnPropertyChanged(nameof(HasStackletItem));
            OnPropertyChanged(nameof(DisplaySummary));
        }

        partial void OnFramesItemChanged(WizardItemOption? value)
        {
            OnPropertyChanged(nameof(HasFramesItem));
            OnPropertyChanged(nameof(DisplaySummary));
        }
        partial void OnFramesDieNumberChanged(string value) => OnPropertyChanged(nameof(DisplaySummary));

        partial void OnDecorationItemChanged(WizardItemOption? value)
        {
            DecorationStampItem = null;
            OnPropertyChanged(nameof(ShowDecorationStampSection));
            OnPropertyChanged(nameof(DisplaySummary));
        }

        partial void OnCuttingMethodChanged(string value) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnPlannedOutItemChanged(WizardItemOption? value) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnStackletDieNumberChanged(string value) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnInsiderItemChanged(WizardItemOption? value) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnInsiderSentimentChanged(string? value) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnFoilItItemChanged(WizardItemOption? value) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnHasDecorationChanged(bool value) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnDecorationStampItemChanged(WizardItemOption? value) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnSelectedCardstockColorChanged(string? value)
        {
            OnPropertyChanged(nameof(EffectiveCardstockColor));
            OnPropertyChanged(nameof(DisplaySummary));
        }
        partial void OnOtherCardstockTextChanged(string value)
        {
            if (SelectedCardstockColor == "Other") OnPropertyChanged(nameof(DisplaySummary));
            OnPropertyChanged(nameof(EffectiveCardstockColor));
        }

        public string DisplaySummary
        {
            get
            {
                // New hub: a piece can have multiple cut-tool picks at the top
                // (Stacklets / Frames / All Planned Out are mutually exclusive among
                // each other; Insider and Foil-It are independent and may coexist
                // alongside any of the cut tools to express "Insider cardstock cut
                // with a Stacklet die"). Enumerate each picked item rather than
                // collapsing to a single CuttingMethod.
                var picks = new List<string>();
                if (PlannedOutItem != null) picks.Add($"All Planned Out: {PlannedOutItem.Name}");
                if (FramesItem != null)
                    picks.Add(string.IsNullOrEmpty(FramesDieNumber)
                        ? $"Frames: {FramesItem.Name}"
                        : $"Frames: {FramesItem.Name} (Die #{FramesDieNumber})");
                if (StackletItem != null)
                    picks.Add(string.IsNullOrEmpty(StackletDieNumber)
                        ? $"Stacklets: {StackletItem.Name}"
                        : $"Stacklets: {StackletItem.Name} (Die #{StackletDieNumber})");
                if (InsiderItem != null)
                    picks.Add(string.IsNullOrEmpty(InsiderSentiment)
                        ? $"Insider: {InsiderItem.Name}"
                        : $"Insider: {InsiderItem.Name} \"{InsiderSentiment}\"");
                if (FoilItItem != null) picks.Add($"Foil-It: {FoilItItem.Name}");
                if (FoilsItem != null)  picks.Add($"Foil sheet: {FoilsItem.Name}");
                if (CuttingMethod == "Custom" && picks.Count == 0) picks.Add("Custom");
                if (CuttingMethod == "None"   && picks.Count == 0) picks.Add("None");

                // Append "die N, X layers" to the cut-tool line if applicable.
                if (CutLayers > 1 || CutDieIndex > 1)
                {
                    var bits = new List<string>();
                    if (CutDieIndex > 1) bits.Add($"die #{CutDieIndex}");
                    if (CutLayers > 1)   bits.Add($"{CutLayers} layers");
                    if (bits.Count > 0 && picks.Count > 0)
                        picks[0] += $" ({string.Join(", ", bits)})";
                }

                string cutting = picks.Count == 0 ? "(no method selected)" : string.Join(" + ", picks);

                foreach (var d in Decorations)
                    cutting += $" + {d.DisplaySummary}";
                if (Adhesives.Count > 0)
                    cutting += $" | Attached: {string.Join(", ", Adhesives)}";

                string main;
                if (!string.IsNullOrEmpty(EffectiveCardstockColor))
                {
                    var cs = EffectiveCardstockColor;
                    if (IsSelfBlended)
                    {
                        cs += " (custom color";
                        if (!string.IsNullOrEmpty(SelfBlendDescription)) cs += $": {SelfBlendDescription}";
                        if (BlendInkColors.Count > 0) cs += $"; inks: {string.Join(", ", BlendInkColors)}";
                        cs += ")";
                    }
                    main = $"{cs} | {cutting}";
                }
                else
                {
                    main = cutting;
                }
                return main;
            }
        }

        public IEnumerable<int> GetItemIds()
        {
            // The new BG-mat hub lets the user combine multiple cut-tool picks on a
            // single piece (e.g. Stacklets die + Insider variant + foil sheet). The
            // summary code (DisplaySummary above) iterates each picker independently
            // rather than gating on a single CuttingMethod — yield items the same
            // way so every picked item lands in the project's items-used list.
            if (PlannedOutItem != null) yield return PlannedOutItem.Id;
            if (FramesItem     != null) yield return FramesItem.Id;
            if (StackletItem   != null) yield return StackletItem.Id;
            if (InsiderItem    != null) yield return InsiderItem.Id;
            if (FoilItItem     != null) yield return FoilItItem.Id;
            if (FoilsItem      != null) yield return FoilsItem.Id;   // foil sheet item, missed before
            foreach (var d in Decorations)
            {
                yield return d.Item.Id;
                if (d.StampItem != null) yield return d.StampItem.Id;
            }
            // Per-piece detail entries (Stamps/Dies/Embell/Stacklets/EF/Stencils/OLO/Foils/etc.
            // each carry their own follow-up picks like glitter items, foil-stencil items,
            // etc.). Iterating them here means every picked item is rolled up into the
            // project's items-used list, in the order the user added them.
            foreach (var det in AddedDetails)
                foreach (var id in det.GetItemIds())
                    yield return id;
        }
    }

    public partial class WizardFocalSection : ObservableObject
    {
        public static List<string> CuttingMethodOptions { get; } = new()
            { "Stacklet", "All Planned Out", "Frames", "Insider", "Foil-It", "Dies", "Custom", "None" };

        public int PartNumber { get; set; }

        // Per-piece detail entries (Stamps/Dies/Embell/Stacklets/EF/Stencils +
        // their follow-ups, OLO, Foils with stencil + ink + glitter follow-ups,
        // watercolors, ink colors). Same shape as WizardBgMat.AddedDetails so
        // CollectAllItemIds + the Details panel can roll them up uniformly.
        public ObservableCollection<WizardDetailEntry> AddedDetails { get; } = new();

        [ObservableProperty] private string? _selectedCardstockColor;
        [ObservableProperty] private string _otherCardstockText = string.Empty;

        // Self-blended cardstock
        [ObservableProperty] private bool _isSelfBlended;
        [ObservableProperty] private string _selfBlendDescription = string.Empty;
        public List<string> BlendInkColors { get; } = new();

        public string EffectiveCardstockColor =>
            SelectedCardstockColor == "Other" ? OtherCardstockText : SelectedCardstockColor ?? string.Empty;

        [ObservableProperty] private string _cuttingMethod = "Stacklet";

        // All Planned Out
        [ObservableProperty] private WizardItemOption? _plannedOutItem;

        // Frames (uses Dies items filtered to subtype "Frames")
        [ObservableProperty] private WizardItemOption? _framesItem;
        [ObservableProperty] private string _framesDieNumber = string.Empty;
        public bool HasFramesItem => FramesItem != null;

        // Stacklet
        [ObservableProperty] private WizardItemOption? _stackletItem;
        [ObservableProperty] private string _stackletDieNumber = string.Empty;
        public bool HasStackletItem => StackletItem != null;

        // Insider
        [ObservableProperty] private WizardItemOption? _insiderItem;
        [ObservableProperty] private string? _insiderSentiment;

        // Foil-It
        [ObservableProperty] private WizardItemOption? _foilItItem;

        // Dies
        [ObservableProperty] private WizardItemOption? _selectedDie;

        // Decoration
        [ObservableProperty] private bool _hasDecoration;
        [ObservableProperty] private WizardItemOption? _decorationItem;
        [ObservableProperty] private WizardItemOption? _decorationStampItem;
        public List<WizardStencilLayer> StencilInkLayers { get; } = new();
        public List<string> StampInkColors { get; } = new();
        public List<string> EmbossingInkColors { get; } = new();
        public List<string> Adhesives { get; } = new();
        public ObservableCollection<WizardMatDecoration> Decorations { get; } = new();

        public bool ShowDecorationStampSection =>
            DecorationItem?.Subtype?.Contains("Embossing Powder", StringComparison.OrdinalIgnoreCase) ?? false;

        // Backer
        [ObservableProperty] private bool _hasBacker;
        [ObservableProperty] private WizardItemOption? _backerItem;
        [ObservableProperty] private string? _backerCardstockColor;
        [ObservableProperty] private string _otherBackerCardstockText = string.Empty;

        public string EffectiveBackerCardstockColor =>
            BackerCardstockColor == "Other" ? OtherBackerCardstockText : BackerCardstockColor ?? string.Empty;

        partial void OnIsSelfBlendedChanged(bool v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnSelfBlendDescriptionChanged(string v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnSelectedCardstockColorChanged(string? v)
        {
            OnPropertyChanged(nameof(EffectiveCardstockColor));
            OnPropertyChanged(nameof(DisplaySummary));
        }
        partial void OnOtherCardstockTextChanged(string v)
        {
            if (SelectedCardstockColor == "Other") OnPropertyChanged(nameof(DisplaySummary));
            OnPropertyChanged(nameof(EffectiveCardstockColor));
        }
        partial void OnCuttingMethodChanged(string v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnPlannedOutItemChanged(WizardItemOption? v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnStackletItemChanged(WizardItemOption? v) { OnPropertyChanged(nameof(HasStackletItem)); OnPropertyChanged(nameof(DisplaySummary)); }
        partial void OnStackletDieNumberChanged(string v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnFramesItemChanged(WizardItemOption? v) { OnPropertyChanged(nameof(HasFramesItem)); OnPropertyChanged(nameof(DisplaySummary)); }
        partial void OnFramesDieNumberChanged(string v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnInsiderItemChanged(WizardItemOption? v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnInsiderSentimentChanged(string? v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnFoilItItemChanged(WizardItemOption? v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnSelectedDieChanged(WizardItemOption? v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnHasDecorationChanged(bool v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnDecorationItemChanged(WizardItemOption? value)
        {
            DecorationStampItem = null;
            OnPropertyChanged(nameof(ShowDecorationStampSection));
            OnPropertyChanged(nameof(DisplaySummary));
        }
        partial void OnDecorationStampItemChanged(WizardItemOption? v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnHasBackerChanged(bool v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnBackerItemChanged(WizardItemOption? v) => OnPropertyChanged(nameof(DisplaySummary));
        partial void OnBackerCardstockColorChanged(string? v)
        {
            OnPropertyChanged(nameof(EffectiveBackerCardstockColor));
            OnPropertyChanged(nameof(DisplaySummary));
        }
        partial void OnOtherBackerCardstockTextChanged(string v)
        {
            if (BackerCardstockColor == "Other") OnPropertyChanged(nameof(DisplaySummary));
            OnPropertyChanged(nameof(EffectiveBackerCardstockColor));
        }

        public string DisplaySummary
        {
            get
            {
                string main = CuttingMethod switch
                {
                    "All Planned Out" => PlannedOutItem?.Name ?? "(none selected)",
                    "Frames" when !string.IsNullOrEmpty(FramesDieNumber) => $"{FramesItem?.Name} (Die #{FramesDieNumber})",
                    "Frames" => FramesItem?.Name ?? "(none selected)",
                    "Stacklet" when !string.IsNullOrEmpty(StackletDieNumber) => $"{StackletItem?.Name} (Die #{StackletDieNumber})",
                    "Stacklet" => StackletItem?.Name ?? "(none selected)",
                    "Insider" when !string.IsNullOrEmpty(InsiderSentiment) => $"{InsiderItem?.Name} \"{InsiderSentiment}\"",
                    "Insider" => InsiderItem?.Name ?? "(none selected)",
                    "Foil-It" => FoilItItem?.Name ?? "(none selected)",
                    "Dies" => SelectedDie?.Name ?? "(none selected)",
                    "Custom" => "Custom",
                    _ => CuttingMethod
                };
                if (!string.IsNullOrEmpty(EffectiveCardstockColor))
                {
                    var cs = EffectiveCardstockColor;
                    if (IsSelfBlended)
                    {
                        cs += " (custom color";
                        if (!string.IsNullOrEmpty(SelfBlendDescription)) cs += $": {SelfBlendDescription}";
                        if (BlendInkColors.Count > 0) cs += $"; inks: {string.Join(", ", BlendInkColors)}";
                        cs += ")";
                    }
                    main = $"{cs} | {main}";
                }
                foreach (var d in Decorations)
                    main += $" + {d.DisplaySummary}";
                if (HasBacker && BackerItem != null)
                {
                    main += $" | Backer: {BackerItem.Name}";
                    if (!string.IsNullOrEmpty(EffectiveBackerCardstockColor)) main += $" on {EffectiveBackerCardstockColor}";
                }
                if (Adhesives.Count > 0)
                    main += $" | Attached: {string.Join(", ", Adhesives)}";
                return main;
            }
        }

        public IEnumerable<int> GetItemIds()
        {
            if (CuttingMethod == "All Planned Out" && PlannedOutItem != null) yield return PlannedOutItem.Id;
            if (CuttingMethod == "Frames" && FramesItem != null) yield return FramesItem.Id;
            if (CuttingMethod == "Stacklet" && StackletItem != null) yield return StackletItem.Id;
            if (CuttingMethod == "Insider" && InsiderItem != null) yield return InsiderItem.Id;
            if (CuttingMethod == "Foil-It" && FoilItItem != null) yield return FoilItItem.Id;
            if (CuttingMethod == "Dies" && SelectedDie != null) yield return SelectedDie.Id;
            foreach (var d in Decorations)
            {
                yield return d.Item.Id;
                if (d.StampItem != null) yield return d.StampItem.Id;
            }
            if (HasBacker && BackerItem != null) yield return BackerItem.Id;
            foreach (var det in AddedDetails)
                foreach (var id in det.GetItemIds())
                    yield return id;
        }
    }

    public partial class WizardBgMatGroup : ObservableObject
    {
        public int GroupNumber { get; set; }
        public string TypeLabel { get; set; } = "Background";
        public ObservableCollection<WizardBgMat> Pieces { get; } = new();
        // Tagged true when the user added this group while IsInsideMode was active.
        // Drives the "Inside " prefix in summary lines + lets reports distinguish
        // outside vs inside selections without a parallel collection.
        public bool IsInside { get; set; }

        public string DisplaySummary
        {
            get
            {
                if (Pieces.Count == 0) return "(empty)";
                var header = $"{TypeLabel} Mat {GroupNumber}:";
                var pieces = string.Join("\n", Pieces.Select((p, i) => $"Piece {i + 1}: {p.DisplaySummary}"));
                return $"{header}\n{pieces}";
            }
        }

        public void NotifyDisplaySummaryChanged() => OnPropertyChanged(nameof(DisplaySummary));
    }

    public partial class WizardSentimentSelection : ObservableObject
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Subtype { get; set; }
        public string? ItemType { get; set; }
        public string? ThumbnailBase64 { get; set; }
        public string? SentimentPreview { get; set; }
        [ObservableProperty] private bool _isSelected;
    }

    public class WizardConfiguredSentimentPart
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? ThumbnailBase64 { get; set; }
        public bool IsStampType { get; set; }
        public string? CardstockColor { get; set; }
        public bool IsSelfBlended { get; set; }
        public string SelfBlendDescription { get; set; } = string.Empty;
        public List<string> BlendInkColors { get; set; } = new();
        public List<string> StampInkColors { get; } = new();
        public bool IsEmbossed { get; set; }
        public string? EmbossingPowderName { get; set; }
        /// <summary>Inventory ID of the embossing powder used on this sentiment part,
        /// captured at save time so the powder gets counted in the project's items-used
        /// roll-up. Was previously dropped because only the name was stored.</summary>
        public int? EmbossingPowderItemId { get; set; }
        public List<string> Adhesives { get; } = new();
        public List<WizardMatDecoration> Decorations { get; } = new();
        // Captured from the new Details sub-page when the sentiment hub routes there.
        // Coexists with legacy Decorations (kept for the old in-line sentiment form).
        public ObservableCollection<WizardDetailEntry> AddedDetails { get; } = new();

        public string DisplaySummary
        {
            get
            {
                var sb = new System.Text.StringBuilder(ItemName);
                if (!string.IsNullOrEmpty(CardstockColor))
                {
                    var cs = CardstockColor;
                    if (IsSelfBlended)
                    {
                        cs += " (self blended";
                        if (!string.IsNullOrEmpty(SelfBlendDescription)) cs += $": {SelfBlendDescription}";
                        if (BlendInkColors.Count > 0) cs += $"; inks: {string.Join(", ", BlendInkColors)}";
                        cs += ")";
                    }
                    sb.Append(IsStampType ? $" on {cs}" : $" with {cs}");
                }
                if (IsStampType)
                {
                    var withParts = new List<string>();
                    if (StampInkColors.Count > 0) withParts.Add(string.Join(", ", StampInkColors));
                    if (IsEmbossed)
                        withParts.Add(!string.IsNullOrEmpty(EmbossingPowderName)
                            ? $"embossed with {EmbossingPowderName}"
                            : "embossed");
                    if (withParts.Count > 0) sb.Append($" with {string.Join(" and ", withParts)}");
                }
                if (Decorations.Count > 0)
                    sb.Append($" + {string.Join(" + ", Decorations.Select(d => d.DisplaySummary))}");
                if (Adhesives.Count > 0) sb.Append($" | Attached: {string.Join(", ", Adhesives)}");
                return sb.ToString();
            }
        }
    }

    public class WizardConfiguredSentiment
    {
        public List<WizardConfiguredSentimentPart> Parts { get; } = new();
        // Tagged true when the user finalized this sentiment while IsInsideMode was on.
        public bool IsInside { get; set; }

        public string DisplaySummary =>
            Parts.Count == 0 ? "(empty)" :
            Parts.Count == 1 ? Parts[0].DisplaySummary :
            string.Join(" + ", Parts.Select((p, i) => $"[{i + 1}] {p.DisplaySummary}"));
    }

    /// <summary>
    /// One captured "detail entry" from the Details sub-page — a snapshot of all
    /// 8 picker selections at the moment Save was clicked. The Details page can
    /// hold any number of these per parent context.
    /// </summary>
    public class WizardDetailEntry
    {
        // Stamp + its follow-ups
        public WizardItemOption? Stamp { get; set; }
        public List<string> StampInkColors { get; set; } = new();
        public bool StampWasEmbossed { get; set; }
        public WizardItemOption? StampEmbossingPowder { get; set; }
        public bool StampUsedAsCombo { get; set; }      // only meaningful when stamp's subtype contains "Die Combo"
        public int StampComboLayers { get; set; } = 1;  // layers when stamp+die combo was used

        // Die + its follow-ups
        public WizardItemOption? Die { get; set; }
        public bool DieIsMultiLayer { get; set; }
        public int DieLayers { get; set; } = 1;

        // Embellishment + (only when Embossing Powder subtype) follow-ups
        public WizardItemOption? Embellishment { get; set; }
        public List<string> EmbellEmbossingInkColors { get; set; } = new();
        public WizardItemOption? EmbellEmbossingStamp { get; set; }

        // Stacklet + its follow-ups
        public WizardItemOption? Stacklet { get; set; }
        public int StackletDieNumber { get; set; } = 1; // 1 = largest
        public int StackletLayers { get; set; } = 1;

        // No-follow-up pickers
        public WizardItemOption? EmbossingFolder { get; set; }
        /// <summary>OLO markers used on this mat detail. Multi-select picker —
        /// stored as a list so multiple markers can be applied to a single mat.</summary>
        public List<WizardItemOption> OloMarkers { get; set; } = new();
        public WizardItemOption? Watercolor { get; set; }
        public string? InkColor { get; set; }

        // Stencil + its follow-ups: ink colors (multi-select, mirrors stamps) plus three
        // special-media toggles (Glitter / Happy Medium / Astro Paste), each with a
        // comma-separated list of stencil layer numbers it was applied to (e.g. "1,3").
        public WizardItemOption? Stencil { get; set; }
        // Per-layer captures from the stencil layer stepper. One entry per physical
        // layer of the picked stencil; each carries its own inks + special-media
        // flags. StencilInkColors below is kept as a flat aggregate for any callers
        // that haven't been migrated to per-layer reads yet.
        public List<WizardStencilLayer> StencilLayerEntries { get; set; } = new();
        public List<string> StencilInkColors { get; set; } = new();
        public bool StencilUsedGlitter { get; set; }
        public string StencilGlitterLayers { get; set; } = string.Empty;
        public bool StencilUsedHappyMedium { get; set; }
        public string StencilHappyMediumLayers { get; set; } = string.Empty;
        public bool StencilUsedAstroPaste { get; set; }
        public string StencilAstroPasteLayers { get; set; } = string.Empty;
        /// <summary>Specific embellishments (subtype Glitter) the user applied to the
        /// stencil. Multi-select — populated only when StencilUsedGlitter is true.</summary>
        public List<WizardItemOption> StencilGlitterItems { get; set; } = new();
        public List<WizardItemOption> StencilHappyMediumItems { get; set; } = new();
        public List<WizardItemOption> StencilAstroPasteItems { get; set; } = new();

        // Foil + its application-method follow-up. Method is "GlitterGrab" or "Toner".
        // GlitterGrab borrows the stencil + ink + glitter/HM/AP pattern from above.
        // Toner takes free text + a font name (with custom-font fallback).
        public WizardItemOption? Foil { get; set; }
        public string FoilApplicationMethod { get; set; } = string.Empty;
        public WizardItemOption? FoilStencil { get; set; }
        public List<string> FoilStencilInkColors { get; set; } = new();
        public bool FoilStencilUsedGlitter { get; set; }
        public bool FoilStencilUsedHappyMedium { get; set; }
        public bool FoilStencilUsedAstroPaste { get; set; }
        public string FoilStencilGlitterLayers     { get; set; } = string.Empty;
        public string FoilStencilHappyMediumLayers { get; set; } = string.Empty;
        public string FoilStencilAstroPasteLayers  { get; set; } = string.Empty;
        public List<WizardItemOption> FoilStencilGlitterItems { get; set; } = new();
        public List<WizardItemOption> FoilStencilHappyMediumItems { get; set; } = new();
        public List<WizardItemOption> FoilStencilAstroPasteItems { get; set; } = new();
        public string FoilTonerText { get; set; } = string.Empty;
        public string FoilTonerFont { get; set; } = string.Empty;

        public IEnumerable<int> GetItemIds()
        {
            if (Stamp != null) yield return Stamp.Id;
            if (StampEmbossingPowder != null) yield return StampEmbossingPowder.Id;
            if (Die != null) yield return Die.Id;
            if (Embellishment != null) yield return Embellishment.Id;
            if (EmbellEmbossingStamp != null) yield return EmbellEmbossingStamp.Id;
            if (Stacklet != null) yield return Stacklet.Id;
            if (EmbossingFolder != null) yield return EmbossingFolder.Id;
            if (Stencil != null) yield return Stencil.Id;
            foreach (var g in StencilGlitterItems) yield return g.Id;
            foreach (var h in StencilHappyMediumItems) yield return h.Id;
            foreach (var a in StencilAstroPasteItems) yield return a.Id;
            // Per-layer stencil items (new layer-stepper). Each layer's
            // Glitter / Happy Medium / Astro Paste picks log every selected
            // inventory item against this build for the used-items report.
            foreach (var layer in StencilLayerEntries)
            {
                foreach (var g in layer.GlitterItems)     yield return g.Id;
                foreach (var h in layer.HappyMediumItems) yield return h.Id;
                foreach (var a in layer.AstroPasteItems)  yield return a.Id;
            }
            foreach (var m in OloMarkers) yield return m.Id;
            if (Foil != null) yield return Foil.Id;
            if (FoilStencil != null) yield return FoilStencil.Id;
            foreach (var g in FoilStencilGlitterItems) yield return g.Id;
            foreach (var h in FoilStencilHappyMediumItems) yield return h.Id;
            foreach (var a in FoilStencilAstroPasteItems) yield return a.Id;
            if (Watercolor != null) yield return Watercolor.Id;
        }

        // Compact one-line description for chips and summary lines.
        public string DisplaySummary
        {
            get
            {
                var parts = new List<string>();
                if (Stamp != null)
                {
                    var s = $"Stamp: {Stamp.Name}";
                    if (StampInkColors.Count > 0) s += $" [{string.Join(", ", StampInkColors)}]";
                    if (StampWasEmbossed && StampEmbossingPowder != null) s += $" + emb. powder: {StampEmbossingPowder.Name}";
                    if (StampUsedAsCombo) s += $" + die ({StampComboLayers} layer{(StampComboLayers != 1 ? "s" : "")})";
                    parts.Add(s);
                }
                if (Die != null)
                {
                    var s = $"Die: {Die.Name}";
                    if (DieIsMultiLayer) s += $" ({DieLayers} layer{(DieLayers != 1 ? "s" : "")})";
                    parts.Add(s);
                }
                if (Embellishment != null)
                {
                    var s = $"Embell: {Embellishment.Name}";
                    if (EmbellEmbossingInkColors.Count > 0) s += $" [{string.Join(", ", EmbellEmbossingInkColors)}]";
                    if (EmbellEmbossingStamp != null) s += $" w/ stamp: {EmbellEmbossingStamp.Name}";
                    parts.Add(s);
                }
                if (Stacklet != null)
                {
                    parts.Add($"Stacklet: {Stacklet.Name} (die #{StackletDieNumber}, {StackletLayers} layer{(StackletLayers != 1 ? "s" : "")})");
                }
                if (EmbossingFolder != null)  parts.Add($"EF: {EmbossingFolder.Name}");
                if (Stencil != null)
                {
                    var s = $"Stencil: {Stencil.Name}";
                    // Prefer the per-layer breakdown (new stepper); fall back to
                    // the legacy flat StencilInkColors list when no per-layer data
                    // was captured (older entries).
                    var nonEmptyLayers = StencilLayerEntries
                        .Where(le => le.InkColors.Count > 0 || le.UsedGlitter || le.UsedHappyMedium || le.UsedAstroPaste)
                        .ToList();
                    if (nonEmptyLayers.Count > 0)
                    {
                        s += " [" + string.Join(" / ", nonEmptyLayers.Select(le => le.DisplaySummary)) + "]";
                    }
                    else if (StencilInkColors.Count > 0)
                    {
                        s += $" [{string.Join(", ", StencilInkColors)}]";
                    }
                    if (StencilUsedGlitter)
                    {
                        s += " + Glitter";
                        if (!string.IsNullOrWhiteSpace(StencilGlitterLayers)) s += $" (layers {StencilGlitterLayers})";
                    }
                    if (StencilUsedHappyMedium)
                    {
                        s += " + Happy Medium";
                        if (!string.IsNullOrWhiteSpace(StencilHappyMediumLayers)) s += $" (layers {StencilHappyMediumLayers})";
                    }
                    if (StencilUsedAstroPaste)
                    {
                        s += " + Astro Paste";
                        if (!string.IsNullOrWhiteSpace(StencilAstroPasteLayers)) s += $" (layers {StencilAstroPasteLayers})";
                    }
                    parts.Add(s);
                }
                if (OloMarkers.Count > 0)     parts.Add($"OLO: {string.Join(", ", OloMarkers.Select(m => m.Name))}");
                if (Watercolor != null)       parts.Add($"Watercolor: {Watercolor.Name}");
                if (!string.IsNullOrEmpty(InkColor)) parts.Add($"Ink: {InkColor}");
                return parts.Count == 0 ? "(empty)" : string.Join(" • ", parts);
            }
        }
    }

    /// <summary>
    /// Toggleable color chip used in multi-select ink color pickers
    /// (stamp inks, embossing-powder inks for embellishments).
    /// </summary>
    public partial class InkColorChip : ObservableObject
    {
        public string Color { get; init; } = string.Empty;
        // Mini Cube / Full Pad ink item id used by LazyThumbnailConverter.
        // 0 means no thumbnail available - the row falls back to a colored placeholder.
        public int ItemId { get; init; }
        [ObservableProperty] private bool _isSelected;
    }

    /// <summary>
    /// Backing VM for the reusable multi-select ink dropdown. Holds the full chip
    /// list and a click-ordered list of selected colors. Subscribe each chip's
    /// PropertyChanged so toggling automatically updates the ordered list.
    /// </summary>
    public partial class InkSelection : ObservableObject
    {
        public ObservableCollection<InkColorChip> Chips { get; } = new();
        // Watercolor chips revealed when the user toggles "Custom Color" mode in the
        // popup. Each chip carries the watercolor item's name + Id so the rows show
        // their inventory thumbnail. Picks land in the same Ordered list, prefixed
        // with "Watercolor: " so the summary keeps them distinguishable from inks.
        public ObservableCollection<InkColorChip> WatercolorChips { get; } = new();
        public ObservableCollection<string> Ordered { get; } = new();

        // Search/filter for the popup. Bound to a TextBox at the top so users with
        // 100+ inks can type instead of scrolling. Filters by Color contains.
        [ObservableProperty] private string _searchText = string.Empty;
        public IEnumerable<InkColorChip> FilteredChips =>
            string.IsNullOrWhiteSpace(SearchText)
                ? Chips
                : Chips.Where(c => c.Color != null && c.Color.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        public IEnumerable<InkColorChip> FilteredWatercolorChips =>
            string.IsNullOrWhiteSpace(SearchText)
                ? WatercolorChips
                : WatercolorChips.Where(c => c.Color != null && c.Color.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        partial void OnSearchTextChanged(string value)
        {
            OnPropertyChanged(nameof(FilteredChips));
            OnPropertyChanged(nameof(FilteredWatercolorChips));
        }

        // Comma-separated list shown on the dropdown's closed face. Empty when nothing's picked.
        public string DisplaySummary => Ordered.Count == 0 ? string.Empty : string.Join(", ", Ordered);
        public bool HasSelection => Ordered.Count > 0;
        public bool HasWatercolorOptions => WatercolorChips.Count > 0;

        [ObservableProperty] private bool _isOpen;
        // Used in the stamp context only — toggled via the "Was this embossed?" chip
        // at the top of the popup chip strip. The control auto-hides this chip when
        // the consumer doesn't enable it.
        [ObservableProperty] private bool _isEmbossed;
        // Toggled via "Custom Color" chip in the popup. While true, watercolor rows
        // are visible alongside the standard ink rows so the user can build a custom
        // blend from BOTH sources.
        [ObservableProperty] private bool _isCustomColorMode;

        public void SetColors(IEnumerable<string> colors, Func<string, int>? itemIdLookup = null)
        {
            // Detach old subscriptions to avoid leaks across reloads
            foreach (var oldChip in Chips) oldChip.PropertyChanged -= OnChipChanged;
            Chips.Clear();
            Ordered.Clear();
            foreach (var c in colors)
            {
                var chip = new InkColorChip
                {
                    Color = c,
                    ItemId = itemIdLookup?.Invoke(c) ?? 0
                };
                chip.PropertyChanged += OnChipChanged;
                Chips.Add(chip);
            }
            OnPropertyChanged(nameof(DisplaySummary));
            OnPropertyChanged(nameof(HasSelection));

            // (Desktop warms a thumbnail cache here; MAUI renders images straight
            // from each option's ImageUrl, so no preload is needed.)
        }

        // Populate the watercolor chips revealed in Custom Color mode. Pass the same
        // WizardItemOption list the WatercolorsPicker uses; each item becomes a chip
        // whose Color is the item's Name and ItemId is the inventory Id (for thumbnail).
        public void SetWatercolors(IEnumerable<WizardItemOption> items)
        {
            foreach (var oldChip in WatercolorChips) oldChip.PropertyChanged -= OnChipChanged;
            WatercolorChips.Clear();
            foreach (var it in items)
            {
                if (it == null) continue;
                var chip = new InkColorChip { Color = it.Name, ItemId = it.Id };
                chip.PropertyChanged += OnChipChanged;
                WatercolorChips.Add(chip);
            }
            OnPropertyChanged(nameof(HasWatercolorOptions));
        }

        public void Clear()
        {
            foreach (var chip in Chips) chip.IsSelected = false;
            foreach (var chip in WatercolorChips) chip.IsSelected = false;
            Ordered.Clear();
            IsEmbossed = false;
            IsCustomColorMode = false;
            OnPropertyChanged(nameof(DisplaySummary));
            OnPropertyChanged(nameof(HasSelection));
        }

        [RelayCommand]
        private void ConfirmAndClose() => IsOpen = false;

        private void OnChipChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(InkColorChip.IsSelected) || sender is not InkColorChip chip) return;
            if (chip.IsSelected)
            {
                if (!Ordered.Contains(chip.Color)) Ordered.Add(chip.Color);
            }
            else
            {
                Ordered.Remove(chip.Color);
            }
            OnPropertyChanged(nameof(DisplaySummary));
            OnPropertyChanged(nameof(HasSelection));
        }
    }

    /// <summary>
    /// Per-layer stencil entry for the wizard's stencil layer stepper. One entry
    /// per physical stencil layer (1..StencilLayers); each owns its own InkSelection
    /// + Glitter/Happy Medium/Astro Paste flags + dedicated multi-select pickers
    /// for the specific items used on that layer. Switching layers in the stepper
    /// just rebinds the UI to a different entry, so each layer's checkbox/picker
    /// state is preserved (or empty for an unedited layer).
    /// </summary>
    public partial class WizardStencilLayerEntry : ObservableObject
    {
        public int LayerNumber { get; set; }
        public InkSelection Inks { get; } = new();
        [ObservableProperty] private bool _usedGlitter;
        [ObservableProperty] private bool _usedHappyMedium;
        [ObservableProperty] private bool _usedAstroPaste;

        // Per-layer item pickers — multi-select so the user can record more than
        // one glitter / Happy Medium / Astro Paste item per layer.
        public WizardItemPicker GlitterPicker      { get; } = new() { PlaceholderText = "Which glitter?",       IsMultiSelect = true };
        public WizardItemPicker HappyMediumPicker  { get; } = new() { PlaceholderText = "Which happy medium?", IsMultiSelect = true };
        public WizardItemPicker AstroPastePicker   { get; } = new() { PlaceholderText = "Which astro paste?",  IsMultiSelect = true };

        public WizardStencilLayerEntry()
        {
            Inks.Ordered.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(SummaryLine));
                OnPropertyChanged(nameof(HasAnything));
            };
            GlitterPicker.SelectedItems.CollectionChanged     += (_, _) => OnPropertyChanged(nameof(SummaryLine));
            HappyMediumPicker.SelectedItems.CollectionChanged += (_, _) => OnPropertyChanged(nameof(SummaryLine));
            AstroPastePicker.SelectedItems.CollectionChanged  += (_, _) => OnPropertyChanged(nameof(SummaryLine));
        }

        partial void OnUsedGlitterChanged(bool v)
        {
            if (!v) GlitterPicker.SelectedItems.Clear();
            OnPropertyChanged(nameof(SummaryLine));
            OnPropertyChanged(nameof(HasAnything));
        }
        partial void OnUsedHappyMediumChanged(bool v)
        {
            if (!v) HappyMediumPicker.SelectedItems.Clear();
            OnPropertyChanged(nameof(SummaryLine));
            OnPropertyChanged(nameof(HasAnything));
        }
        partial void OnUsedAstroPasteChanged(bool v)
        {
            if (!v) AstroPastePicker.SelectedItems.Clear();
            OnPropertyChanged(nameof(SummaryLine));
            OnPropertyChanged(nameof(HasAnything));
        }

        public bool HasAnything =>
            Inks.HasSelection || UsedGlitter || UsedHappyMedium || UsedAstroPaste;

        public string SummaryLine
        {
            get
            {
                var bits = new List<string>();
                if (Inks.HasSelection) bits.Add(string.Join(", ", Inks.Ordered));
                if (UsedGlitter)
                {
                    var label = "Glitter";
                    if (GlitterPicker.SelectedItems.Count > 0)
                        label += $" ({string.Join(", ", GlitterPicker.SelectedItems.Select(i => i.Name))})";
                    bits.Add(label);
                }
                if (UsedHappyMedium)
                {
                    var label = "Happy Medium";
                    if (HappyMediumPicker.SelectedItems.Count > 0)
                        label += $" ({string.Join(", ", HappyMediumPicker.SelectedItems.Select(i => i.Name))})";
                    bits.Add(label);
                }
                if (UsedAstroPaste)
                {
                    var label = "Astro Paste";
                    if (AstroPastePicker.SelectedItems.Count > 0)
                        label += $" ({string.Join(", ", AstroPastePicker.SelectedItems.Select(i => i.Name))})";
                    bits.Add(label);
                }
                return bits.Count == 0
                    ? $"Layer {LayerNumber}: (nothing yet)"
                    : $"Layer {LayerNumber}: {string.Join(" + ", bits)}";
            }
        }
    }

    /// <summary>
    /// Drop-in reusable VM helper for the wizard's "pick an item" dropdown.
    /// Holds a master list, exposes chip-filtered + search-filtered FilteredItems,
    /// and tracks the selected item. Pair with WizardItemPickerControl.
    /// </summary>
    public partial class WizardItemPicker : ObservableObject
    {
        private readonly List<WizardItemOption> _all = new();

        public ObservableCollection<string> Subtypes { get; } = new();
        public ObservableCollection<WizardItemOption> FilteredItems { get; } = new();

        /// <summary>For multi-select pickers (e.g. OLO Markers in card-build details).
        /// Single-select pickers leave this empty and use SelectedItem instead.</summary>
        public ObservableCollection<WizardItemOption> SelectedItems { get; } = new();

        [ObservableProperty] private string _activeSubtype = "All";
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private WizardItemOption? _selectedItem;
        [ObservableProperty] private bool _isOpen;
        [ObservableProperty] private bool _isMultiSelect;

        // Optional one-line label shown on the closed dropdown when nothing is selected.
        public string PlaceholderText { get; init; } = "Select...";

        public bool HasSubtypes => Subtypes.Count > 1; // "All" + at least one real subtype

        /// <summary>Comma-joined names of all SelectedItems, used as the toggle-button
        /// label for multi-select pickers. Updated whenever SelectedItems changes.</summary>
        public string SelectedItemsLabel
        {
            get
            {
                if (SelectedItems.Count == 0) return string.Empty;
                if (SelectedItems.Count <= 3)
                    return string.Join(", ", SelectedItems.Select(i => i.Name));
                return $"{SelectedItems[0].Name} +{SelectedItems.Count - 1}";
            }
        }

        /// <summary>Unified text shown on the closed picker button. Single-select
        /// pickers show the chosen item's name; multi-select pickers show the
        /// joined SelectedItems label.</summary>
        public string DisplayLabel => IsMultiSelect ? SelectedItemsLabel : (SelectedItem?.Name ?? string.Empty);

        public bool HasSelection => IsMultiSelect ? SelectedItems.Count > 0 : SelectedItem != null;

        public WizardItemPicker()
        {
            SelectedItems.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(SelectedItemsLabel));
                OnPropertyChanged(nameof(DisplayLabel));
                OnPropertyChanged(nameof(HasSelection));
            };
        }

        partial void OnSelectedItemChanged(WizardItemOption? value)
        {
            OnPropertyChanged(nameof(DisplayLabel));
            OnPropertyChanged(nameof(HasSelection));
        }

        partial void OnIsMultiSelectChanged(bool value)
        {
            OnPropertyChanged(nameof(DisplayLabel));
            OnPropertyChanged(nameof(HasSelection));
        }

        /// <summary>Toggle an item in the multi-select set. Used by the picker control
        /// when <see cref="IsMultiSelect"/> is true; clicks add or remove the item
        /// instead of replacing SelectedItem.</summary>
        public void ToggleSelected(WizardItemOption item)
        {
            var existing = SelectedItems.FirstOrDefault(i => i.Id == item.Id);
            if (existing != null) SelectedItems.Remove(existing);
            else SelectedItems.Add(item);
        }

        public bool IsItemSelected(WizardItemOption item) =>
            SelectedItems.Any(i => i.Id == item.Id);

        /// <summary>
        /// Loads items into the picker. If <paramref name="canonicalSubtypes"/> is supplied
        /// (e.g. from UserSettingsService.GetSubtypesForType), only those subtypes appear
        /// as chips — and an item matches a chip when its Subtype field CONTAINS the chip
        /// text (case-insensitive). When omitted, falls back to auto-extracted distinct
        /// subtype values with exact-match filtering.
        /// </summary>
        public void Load(IEnumerable<WizardItemOption> items, IEnumerable<string>? canonicalSubtypes = null)
        {
            _all.Clear();
            _all.AddRange(items);

            Subtypes.Clear();
            Subtypes.Add("All");
            if (canonicalSubtypes != null)
            {
                _useContainsMatch = true;
                foreach (var s in canonicalSubtypes
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                    Subtypes.Add(s);
            }
            else
            {
                _useContainsMatch = false;
                foreach (var s in _all
                    .Select(i => i.Subtype)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
                    Subtypes.Add(s!);
            }
            OnPropertyChanged(nameof(HasSubtypes));

            ActiveSubtype = "All";
            SearchText = string.Empty;
            Refilter();

            // (Desktop warms a thumbnail cache here; MAUI renders images straight
            // from each option's ImageUrl, so no preload is needed.)
        }

        private bool _useContainsMatch;

        partial void OnActiveSubtypeChanged(string value) => Refilter();
        partial void OnSearchTextChanged(string value) => Refilter();

        private void Refilter()
        {
            // Preserve the load-time ordering of _all rather than re-sorting alphabetically here.
            // The caller (e.g. cardstock pinned ordering) controls the sort by passing items
            // already in the desired order — re-sorting in Refilter would clobber that
            // (e.g. dropping "Sugar Cube" from the top because "Banana" sorts earlier).
            IEnumerable<WizardItemOption> q = _all;
            if (!string.Equals(ActiveSubtype, "All", StringComparison.OrdinalIgnoreCase))
            {
                if (_useContainsMatch)
                    q = q.Where(i => i.Subtype != null
                        && i.Subtype.Contains(ActiveSubtype, StringComparison.OrdinalIgnoreCase));
                else
                    q = q.Where(i => string.Equals(i.Subtype, ActiveSubtype, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(SearchText))
                q = q.Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            FilteredItems.Clear();
            foreach (var i in q) FilteredItems.Add(i);
        }

        [RelayCommand]
        private void SelectSubtype(string? subtype)
        {
            if (!string.IsNullOrEmpty(subtype)) ActiveSubtype = subtype;
        }
    }

    /// <summary>
    /// One line in the right-side Summary panel. Carries display text and an optional
    /// remove callback — when non-null, the row renders a ✕ button that invokes it
    /// (used to delete an accidentally-added detail entry from its source collection).
    /// </summary>
    public class SummaryRow
    {
        public string Text { get; init; } = string.Empty;
        public Action? RemoveAction { get; init; }
        public bool IsRemovable => RemoveAction != null;
    }

    public class WizardEmbellishment
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Subtype { get; set; }
        public int? StampItemId { get; set; }
        public string? StampItemName { get; set; }
        // Ink colors used (only meaningful when Subtype contains "Embossing Powder").
        // Captured from the same multi-select ink picker the Details tab uses.
        public List<string> InkColors { get; set; } = new();
        // Tagged true when the user added this embellishment while IsInsideMode was on.
        public bool IsInside { get; set; }

        public string DisplaySummary
        {
            get
            {
                var s = ItemName;
                if (InkColors.Count > 0) s += $" [{string.Join(", ", InkColors)}]";
                if (!string.IsNullOrEmpty(StampItemName)) s += $" (stamp: {StampItemName})";
                return s;
            }
        }
    }

    // ── Main wizard ViewModel ──────────────────────────────────────────────────

}
