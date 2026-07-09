using System.Text.Json;
using System.Text.Json.Serialization;
using MyCraftyStash.Services;
using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Models
{
    /// <summary>
    /// Round-trip-able snapshot of the card build wizard's state. The shape is
    /// IDENTICAL to the Windows desktop app's WizardBuildSnapshot so a build
    /// saved on either app rehydrates on the other.
    /// </summary>
    public class WizardBuildSnapshot
    {
        public string Version { get; set; } = "1";

        // ── Top-level wizard state ────────────────────────────────────────────
        public string SelectedCardBase { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // ── Card Base ─────────────────────────────────────────────────────────
        public int? BaseRegularCardstockItemId { get; set; }
        public int? BaseFoilCardstockItemId { get; set; }
        public int? BaseGlitterCardstockItemId { get; set; }
        public string? BaseCardstockColor { get; set; }
        public bool BaseIsSelfBlended { get; set; }
        public string BaseSelfBlendDescription { get; set; } = string.Empty;
        public List<string> BaseBlendInkColors { get; set; } = new();
        public WizardFocalSection? CardBase { get; set; }
        public List<WizardDetailEntry> CardBaseAddedDetails { get; set; } = new();
        public List<WizardItemOption> CardBaseAddedAdhesives { get; set; } = new();

        // ── Outside Sections ─────────────────────────────────────────────────
        public List<WizardBgMatGroup> BgMats { get; set; } = new();
        public List<WizardBgMatGroup> AdditionalMats { get; set; } = new();
        public List<WizardBgMatGroup> FocalMatGroups { get; set; } = new();
        public List<WizardFocalSection> FocalParts { get; set; } = new();
        public List<WizardConfiguredSentiment> ConfiguredSentiments { get; set; } = new();
        public List<WizardEmbellishment> AddedEmbellishments { get; set; } = new();
        public WizardItemOption? SelectedEnvelopeItem { get; set; }
        public WizardItemOption? SelectedStorageBagItem { get; set; }

        // ── Inside Sections ──────────────────────────────────────────────────
        public List<WizardBgMat> InsideBgMats { get; set; } = new();
        public List<WizardBgMat> InsideAdditionalMats { get; set; } = new();
        public WizardFocalSection? InsideFocal { get; set; }
        public List<WizardConfiguredSentiment> ConfiguredInsideSentiments { get; set; } = new();
        public List<WizardEmbellishment> InsideAddedEmbellishments { get; set; } = new();

        // ── Inside hub: liner cardstock (the inside layer of the card) ───────
        public int? InsideLinerCardstockItemId { get; set; }
        public string? InsideLinerCardstockColor { get; set; }

        // ── Inside hub: top-level Details picks (stamps, dies, etc used on
        //    the inside but not tied to any specific Mat group). Shares the
        //    WizardDetailEntry shape so the same chip-strip UI works. ────────
        public List<WizardDetailEntry> InsideMiscDetails { get; set; } = new();


        public static JsonSerializerOptions JsonOptions { get; } = new()
        {
            WriteIndented = false,
            IgnoreReadOnlyProperties = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            // Populate get-only collections (e.g. ObservableCollection<T> X { get; })
            // or the deserializer silently drops their JSON.
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        };

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public static WizardBuildSnapshot? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<WizardBuildSnapshot>(json, JsonOptions); }
            catch (JsonException) { return null; }
        }
    }
}
