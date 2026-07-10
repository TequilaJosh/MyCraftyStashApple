using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyCraftyStash.ViewModels;

/// <summary>A supply row in the Stock Tracker (item + editable current stock).</summary>
public partial class StockRow : ObservableObject
{
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Subtype { get; set; }
    public int? PackSize { get; set; }

    public bool HasSubtype => !string.IsNullOrWhiteSpace(Subtype);
    public bool HasPackSize => PackSize.HasValue;
    public string PackSizeText => PackSize.HasValue ? $"{PackSize} per pack" : "";

    [ObservableProperty] public partial int CurrentStock { get; set; }
    [ObservableProperty] public partial bool IsEditing { get; set; }
    [ObservableProperty] public partial string EditValueText { get; set; } = "0";

    /// <summary>sheets / envelopes / foils / foil-its / units, per the item type.</summary>
    public string UnitLabel
    {
        get
        {
            var t = Type ?? "";
            if (t.Contains("Envelope", StringComparison.OrdinalIgnoreCase)) return "envelopes";
            if (t.Contains("Cardstock", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Card Bases", StringComparison.OrdinalIgnoreCase) ||
                t.Contains("Watercolor", StringComparison.OrdinalIgnoreCase)) return "sheets";
            if (t.Contains("Foil-it", StringComparison.OrdinalIgnoreCase)) return "foil-its";
            if (t.Contains("Foil", StringComparison.OrdinalIgnoreCase)) return "foils";
            return "units";
        }
    }

    public bool IsOut => CurrentStock == 0;
    partial void OnCurrentStockChanged(int value) => OnPropertyChanged(nameof(IsOut));
}

/// <summary>A type group of supply rows with header counts + total.</summary>
public partial class StockGroup : ObservableObject
{
    public string TypeName { get; set; } = "";
    public ObservableCollection<StockRow> Items { get; } = new();
    public string CountText => Items.Count == 1 ? "1 item" : $"{Items.Count} items";
    public string UnitLabel => Items.FirstOrDefault()?.UnitLabel ?? "units";
    [ObservableProperty] public partial string TotalText { get; set; } = "";

    public void RecomputeTotal() => TotalText = $"Total: {Items.Sum(i => i.CurrentStock)} {UnitLabel}";
}

/// <summary>A tracked-project row (finished-card on-hand count).</summary>
public partial class ProjectStockRow : ObservableObject
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = "";

    [ObservableProperty] public partial int QuantityOnHand { get; set; }
    [ObservableProperty] public partial bool IsEditing { get; set; }
    [ObservableProperty] public partial string EditValueText { get; set; } = "0";

    public bool IsOut => QuantityOnHand == 0;
    partial void OnQuantityOnHandChanged(int value) => OnPropertyChanged(nameof(IsOut));
}
