using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;

namespace MyCraftyStash.Services;

/// <summary>One line in an expense/sales report.</summary>
public class ReportRow
{
    public string ItemName { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public string DateText => Date?.ToString("MMM d, yyyy") ?? "";
    public string UnitPriceText => UnitPrice.ToString("C");
    public string LineTotalText => LineTotal.ToString("C");
    public string QuantityText => $"×{Quantity}";
}

public class ReportResult
{
    public decimal Total { get; set; }
    public List<ReportRow> Rows { get; set; } = new();
}

/// <summary>Aggregates purchase (expense) and sale history for the report views.</summary>
public class ReportService
{
    public async Task<ReportResult> GetExpenseReportAsync()
    {
        using var db = new InventoryDbContext();
        var rows = await db.ItemPurchases.AsNoTracking()
            .Include(p => p.Item)
            .OrderByDescending(p => p.DatePurchased ?? p.CreatedAt)
            .Select(p => new ReportRow
            {
                ItemName = p.Item != null ? p.Item.Name : "(deleted item)",
                Date = p.DatePurchased ?? p.CreatedAt,
                Quantity = p.Quantity,
                UnitPrice = p.PricePerItem,
                LineTotal = p.Quantity * p.PricePerItem,
            })
            .ToListAsync();

        return new ReportResult { Rows = rows, Total = rows.Sum(r => r.LineTotal) };
    }

    public async Task<ReportResult> GetSalesReportAsync()
    {
        using var db = new InventoryDbContext();
        var rows = await db.ItemSales.AsNoTracking()
            .Include(s => s.Item)
            .OrderByDescending(s => s.DateSold ?? s.CreatedAt)
            .Select(s => new ReportRow
            {
                ItemName = s.Item != null ? s.Item.Name : "(deleted item)",
                Date = s.DateSold ?? s.CreatedAt,
                Quantity = s.Quantity,
                UnitPrice = s.SalePrice,
                LineTotal = s.Quantity * s.SalePrice,
            })
            .ToListAsync();

        return new ReportResult { Rows = rows, Total = rows.Sum(r => r.LineTotal) };
    }
}
