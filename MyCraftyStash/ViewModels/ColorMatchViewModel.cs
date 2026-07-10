using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>One chart row with its two ownership signals.</summary>
public class ColorMatchRow
{
    public ColorMatch Match { get; init; } = new();
    public bool IsOwnedTe { get; init; }
    public bool IsOwnedExternal { get; init; }

    public string Code => Match.ExternalCode;
    public string TeColorName => Match.TeColorName;
    public string? Notes => Match.Notes;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
    public string TeGlyph => IsOwnedTe ? "✓" : "○";
    public string ExternalGlyph => IsOwnedExternal ? "✓" : "○";
    public bool IsAnyOwned => IsOwnedTe || IsOwnedExternal;
}

/// <summary>One system's chart (DMC or OLO) with All/Owned/Missing filters,
/// search, and the ownership summary.</summary>
public partial class SystemColorMatchViewModel : ObservableObject
{
    private readonly ColorMatchService _service;
    public string System { get; }
    public string SystemLabel { get; }
    private List<ColorMatchRow> _all = new();

    public SystemColorMatchViewModel(ColorMatchService service, string system, string label)
    {
        _service = service;
        System = system;
        SystemLabel = label;
    }

    public ObservableCollection<ColorMatchRow> Filtered { get; } = new();

    [ObservableProperty] public partial string? SearchText { get; set; }
    [ObservableProperty] public partial string Filter { get; set; } = "All";  // All | Owned | Missing
    [ObservableProperty] public partial int OwnedCount { get; set; }
    [ObservableProperty] public partial int TotalCount { get; set; }
    [ObservableProperty] public partial int ExternalOwnedCount { get; set; }
    [ObservableProperty] public partial string SummaryText { get; set; } = "";
    [ObservableProperty] public partial bool IsEmpty { get; set; }

    public bool IsAll => Filter == "All";
    public bool IsOwned => Filter == "Owned";
    public bool IsMissing => Filter == "Missing";

    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnFilterChanged(string value)
    {
        OnPropertyChanged(nameof(IsAll));
        OnPropertyChanged(nameof(IsOwned));
        OnPropertyChanged(nameof(IsMissing));
        ApplyFilter();
    }

    [RelayCommand] private void SetFilter(string mode) => Filter = mode;

    public async Task LoadAsync()
    {
        var matches = await _service.GetAllAsync(System);
        var ownedTe = await _service.GetOwnedTeColorNamesAsync();
        var ownedExt = await _service.GetOwnedExternalCodeStringsAsync(System);

        _all = matches.Select(m => new ColorMatchRow
        {
            Match = m,
            IsOwnedTe = ownedTe.Contains(m.TeColorName),
            IsOwnedExternal = ColorMatchService.ExternalOwned(m.ExternalCode, ownedExt),
        }).ToList();

        TotalCount = _all.Count;
        OwnedCount = _all.Count(r => r.IsOwnedTe);
        ExternalOwnedCount = _all.Count(r => r.IsOwnedExternal);
        int pct = TotalCount == 0 ? 0 : (int)Math.Round(100.0 * OwnedCount / TotalCount);
        SummaryText = $"You own {OwnedCount} of {TotalCount} TE colors ({pct}%) · {ExternalOwnedCount} matching {SystemLabel} supplies in your inventory";

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<ColorMatchRow> q = _all;
        if (Filter == "Owned") q = q.Where(r => r.IsOwnedTe);
        else if (Filter == "Missing") q = q.Where(r => !r.IsOwnedTe);

        var s = SearchText?.Trim();
        if (!string.IsNullOrEmpty(s))
            q = q.Where(r => r.TeColorName.Contains(s, StringComparison.OrdinalIgnoreCase)
                          || r.Code.Contains(s, StringComparison.OrdinalIgnoreCase));

        Filtered.Clear();
        foreach (var r in q) Filtered.Add(r);
        IsEmpty = Filtered.Count == 0;
    }
}

/// <summary>Color Match — DMC Floss / OLO Marker tabs over the TE colour chart.</summary>
public partial class ColorMatchViewModel : ObservableObject, IRefreshOnReturn
{
    public SystemColorMatchViewModel DmcVM { get; }
    public SystemColorMatchViewModel OloVM { get; }

    public ColorMatchViewModel(ColorMatchService service)
    {
        DmcVM = new SystemColorMatchViewModel(service, ColorMatchService.SystemDmc, "DMC Floss");
        OloVM = new SystemColorMatchViewModel(service, ColorMatchService.SystemOlo, "OLO Marker");
    }

    [ObservableProperty] public partial string ActiveTab { get; set; } = "DMC";
    public bool IsDmcTab => ActiveTab == "DMC";
    public bool IsOloTab => ActiveTab == "OLO";
    partial void OnActiveTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsDmcTab));
        OnPropertyChanged(nameof(IsOloTab));
    }

    [RelayCommand] private void ShowDmc() => ActiveTab = "DMC";
    [RelayCommand] private void ShowOlo() => ActiveTab = "OLO";

    public async Task Refresh()
    {
        await Task.WhenAll(DmcVM.LoadAsync(), OloVM.LoadAsync());
    }
}
