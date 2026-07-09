namespace MyCraftyStash.Services;

// Option DTOs used by the card build wizard's pickers. Shapes match the
// desktop app exactly so WizardBuildSnapshot JSON round-trips across apps.
public class WizardItemOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ItemType { get; set; }
    public string? Subtype { get; set; }
    public int? StencilLayers { get; set; }
    public string? ImageUrl { get; set; }
    public override string ToString() => Subtype != null ? $"{Name} ({Subtype})" : Name;
}

public class WizardDieOption
{
    public int Id { get; set; }
    public int DieNumber { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public override string ToString() => Label;
}

public class WizardSentimentResult
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? SentimentPreview { get; set; }
    public string? ThumbnailBase64 { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>One assembled build step (matches the desktop's record exactly).</summary>
public record WizardBuildStep(
    string Section,
    string StepType,
    int? MatLayer,
    int? ItemId,
    int? StackletDieId,
    string? CuttingMethod,
    string Label);
