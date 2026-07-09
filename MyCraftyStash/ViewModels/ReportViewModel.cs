using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

public enum ReportKind { Expense, Sales }

/// <summary>Shared VM for the Expense and Sales reports — same shape, different
/// source. The view calls Init(kind) when it's shown.</summary>
public partial class ReportViewModel : ObservableObject
{
    private readonly ReportService _service;
    private ReportKind _kind;

    public ReportViewModel(ReportService service) => _service = service;

    public ObservableCollection<ReportRow> Rows { get; } = new();

    [ObservableProperty] public partial string Title { get; set; } = "";
    [ObservableProperty] public partial string TotalLabel { get; set; } = "";
    [ObservableProperty] public partial string TotalText { get; set; } = "";
    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }
    [ObservableProperty] public partial string EmptyText { get; set; } = "";

    public async void Init(ReportKind kind)
    {
        _kind = kind;
        Title = kind == ReportKind.Expense ? "Expense Report" : "Sales Report";
        TotalLabel = kind == ReportKind.Expense ? "Total spent" : "Total earned";
        EmptyText = kind == ReportKind.Expense
            ? "No purchases recorded yet. Add purchase history to an item and it'll show up here."
            : "No sales recorded yet. Record a sale on an item and it'll show up here.";
        await Load();
    }

    [RelayCommand]
    private async Task Load()
    {
        Busy = true;
        try
        {
            var result = _kind == ReportKind.Expense
                ? await _service.GetExpenseReportAsync()
                : await _service.GetSalesReportAsync();
            Rows.Clear();
            foreach (var r in result.Rows)
                Rows.Add(r);
            TotalText = result.Total.ToString("C");
            IsEmpty = Rows.Count == 0;
        }
        finally { Busy = false; }
    }
}
