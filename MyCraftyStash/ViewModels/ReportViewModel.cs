using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

public enum ReportKind { Expense, Sales }

/// <summary>A group card in the report (Month / Type / All), with its rows + totals.</summary>
public class ReportGroup
{
    public string Header { get; set; } = "";
    public string CountText { get; set; } = "";
    public string TotalText { get; set; } = "";
    public List<ReportRow> Rows { get; set; } = new();
}

/// <summary>
/// Expense / Sales report — clone of the desktop's report views: From/To date
/// range + Run Report, Group by (Month/Type/None), four summary cards, a
/// grouped 6-column breakdown, and CSV export. Same shape for both kinds; the
/// labels/accent differ by <see cref="ReportKind"/>.
/// </summary>
public partial class ReportViewModel : ObservableObject
{
    private readonly ReportService _service;
    private ReportKind _kind;
    private List<ReportRow> _allRows = new();

    public ReportViewModel(ReportService service) => _service = service;

    // Header
    [ObservableProperty] public partial string BreadcrumbText { get; set; } = "";
    [ObservableProperty] public partial string Title { get; set; } = "";
    [ObservableProperty] public partial string Subtitle { get; set; } = "";
    [ObservableProperty] public partial string EmptyEmoji { get; set; } = "";

    // Filters
    [ObservableProperty] public partial DateTime FromDate { get; set; } = DateTime.Today.AddMonths(-3);
    [ObservableProperty] public partial DateTime ToDate { get; set; } = DateTime.Today;
    public ObservableCollection<string> GroupByOptions { get; } = new() { "Month", "Type", "None" };
    [ObservableProperty] public partial string SelectedGroupBy { get; set; } = "Month";
    partial void OnSelectedGroupByChanged(string value) => Regroup();

    // Summary cards
    [ObservableProperty] public partial string TotalCardLabel { get; set; } = "";
    [ObservableProperty] public partial string TotalCardValue { get; set; } = "$0.00";
    [ObservableProperty] public partial string ItemsCardLabel { get; set; } = "";
    [ObservableProperty] public partial string ItemsCardValue { get; set; } = "0";
    [ObservableProperty] public partial string UniqueItemsValue { get; set; } = "0";
    [ObservableProperty] public partial string AvgPerItemValue { get; set; } = "$0.00";

    // Results
    public ObservableCollection<ReportGroup> Groups { get; } = new();
    [ObservableProperty] public partial bool HasResults { get; set; }
    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial string StatusMessage { get; set; } = "Choose a date range and tap Run Report.";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    // Sales money is green; expenses use the crimson primary.
    [ObservableProperty] public partial Color AccentColor { get; set; } = Color.FromArgb("#A82C32");

    public void Init(ReportKind kind)
    {
        _kind = kind;
        bool expense = kind == ReportKind.Expense;
        BreadcrumbText = expense ? "My Crafty Stash  /  Expense report" : "My Crafty Stash  /  Sales report";
        Title = expense ? "Purchase history" : "Sales history";
        Subtitle = expense ? "Track and analyze your craft supply spending." : "Track and analyze what you've sold.";
        EmptyEmoji = expense ? "\U0001F4B3" : "\U0001F4B0";
        TotalCardLabel = expense ? "TOTAL SPEND" : "TOTAL REVENUE";
        ItemsCardLabel = expense ? "ITEMS PURCHASED" : "ITEMS SOLD";
        AccentColor = expense ? Color.FromArgb("#A82C32") : Color.FromArgb("#3F8F62");
        _ = RunReport();
    }

    [RelayCommand]
    private async Task RunReport()
    {
        Busy = true;
        ErrorMessage = null;
        try
        {
            var result = _kind == ReportKind.Expense
                ? await _service.GetExpenseReportAsync(FromDate, ToDate)
                : await _service.GetSalesReportAsync(FromDate, ToDate);
            _allRows = result.Rows;

            decimal total = _allRows.Sum(r => r.LineTotal);
            int qty = _allRows.Sum(r => r.Quantity);
            int unique = _allRows.Select(r => r.ItemName).Distinct().Count();
            decimal avg = qty > 0 ? total / qty : 0m;

            TotalCardValue = "$" + total.ToString("N2");
            ItemsCardValue = qty.ToString();
            UniqueItemsValue = unique.ToString();
            AvgPerItemValue = "$" + avg.ToString("N2");

            Regroup();

            if (_allRows.Count == 0)
                StatusMessage = _kind == ReportKind.Expense
                    ? "No purchases found for this period."
                    : "No sales found for this period.";
        }
        catch (Exception ex)
        {
            ErrorMessage = _kind == ReportKind.Expense
                ? "Failed to load purchase history."
                : "Failed to load sales history.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
        finally { Busy = false; }
    }

    private void Regroup()
    {
        Groups.Clear();
        HasResults = _allRows.Count > 0;
        if (!HasResults) return;

        IEnumerable<ReportGroup> groups;
        switch (SelectedGroupBy)
        {
            case "Month":
                groups = _allRows
                    .GroupBy(r => r.Date.HasValue ? new DateTime(r.Date.Value.Year, r.Date.Value.Month, 1) : DateTime.MinValue)
                    .OrderByDescending(g => g.Key)
                    .Select(g => MakeGroup(g.Key == DateTime.MinValue ? "Unknown" : g.Key.ToString("MMMM yyyy"), g));
                break;
            case "Type":
                groups = _allRows
                    .GroupBy(r => string.IsNullOrEmpty(r.ItemType) ? "Unknown" : r.ItemType)
                    .OrderBy(g => g.Key)
                    .Select(g => MakeGroup(g.Key, g));
                break;
            default: // "None"
                var header = _kind == ReportKind.Expense ? "All Purchases" : "All Sales";
                groups = new[] { MakeGroup(header, _allRows) };
                break;
        }
        foreach (var g in groups) Groups.Add(g);
    }

    private ReportGroup MakeGroup(string header, IEnumerable<ReportRow> rows)
    {
        var list = rows.ToList();
        var noun = _kind == ReportKind.Expense ? "purchase(s)" : "sale(s)";
        return new ReportGroup
        {
            Header = header,
            Rows = list,
            CountText = $"{list.Count} {noun}",
            TotalText = "$" + list.Sum(r => r.LineTotal).ToString("N2"),
        };
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        if (_allRows.Count == 0) return;
        try
        {
            var sb = new StringBuilder();
            if (_kind == ReportKind.Expense)
                sb.AppendLine("Item Name,Type,Item Number,Date,Qty,Price Each,Line Total");
            else
                sb.AppendLine("Item Name,Type,Item Number,Date Sold,Qty,Sale Price,Line Total");

            foreach (var r in _allRows)
                sb.AppendLine(string.Join(",", Esc(r.ItemName), Esc(r.ItemType), Esc(r.ItemNumber ?? ""),
                    Esc(r.Date?.ToString("yyyy-MM-dd") ?? ""), r.Quantity, r.UnitPrice.ToString("F2"), r.LineTotal.ToString("F2")));
            sb.AppendLine($"TOTAL,,,,{_allRows.Sum(r => r.Quantity)},,{_allRows.Sum(r => r.LineTotal):F2}");

            var prefix = _kind == ReportKind.Expense ? "PurchaseReport" : "SalesReport";
            var name = $"{prefix}_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.csv";
            var path = Path.Combine(FileSystem.CacheDirectory, name);
            await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Export report",
                File = new ShareFile(path),
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = "Export failed.";
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    /// <summary>CSV cell escaping with formula-injection defense (desktop parity).</summary>
    private static string Esc(string cell)
    {
        if (cell.Length > 0 && "=+-@\t\r".IndexOf(cell[0]) >= 0) cell = "'" + cell;
        if (cell.IndexOfAny(new[] { ',', '"', '\n' }) >= 0) cell = "\"" + cell.Replace("\"", "\"\"") + "\"";
        return cell;
    }
}
