using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;

namespace MyCraftyStash.Services;

/// <summary>One line in an expense/sales report (item + date + qty + prices).</summary>
public class ReportRow
{
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string? ItemNumber { get; set; }
    public DateTime? Date { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public string DateText => Date?.ToString("MMM d, yyyy") ?? "-";
    public string ItemNumberText => string.IsNullOrEmpty(ItemNumber) ? "" : $"#{ItemNumber}";
    public string UnitPriceText => "$" + UnitPrice.ToString("F2");
    public string LineTotalText => "$" + LineTotal.ToString("F2");
    public string QuantityText => Quantity.ToString();
}

public class ReportResult
{
    public List<ReportRow> Rows { get; set; } = new();
}

/// <summary>Aggregates purchase (expense) and sale history over a date range.</summary>
public class ReportService
{
    public async Task<ReportResult> GetExpenseReportAsync(DateTime? from, DateTime? to)
    {
        using var db = new InventoryDbContext();
        IQueryable<Models.ItemPurchase> q = db.ItemPurchases.AsNoTracking().Include(p => p.Item);
        if (from.HasValue) q = q.Where(p => (p.DatePurchased ?? p.CreatedAt) >= from.Value);
        if (to.HasValue) { var upper = to.Value.Date.AddDays(1); q = q.Where(p => (p.DatePurchased ?? p.CreatedAt) < upper); }

        var rows = await q
            .OrderByDescending(p => p.DatePurchased ?? p.CreatedAt)
            .Select(p => new ReportRow
            {
                ItemName = p.Item != null ? p.Item.Name : "(deleted item)",
                ItemType = p.Item != null ? p.Item.Type : "",
                ItemNumber = p.Item != null ? p.Item.ItemNumber : null,
                Date = p.DatePurchased ?? p.CreatedAt,
                Quantity = p.Quantity,
                UnitPrice = p.PricePerItem,
                LineTotal = p.Quantity * p.PricePerItem,
            })
            .ToListAsync();

        return new ReportResult { Rows = rows };
    }

    public async Task<ReportResult> GetSalesReportAsync(DateTime? from, DateTime? to)
    {
        using var db = new InventoryDbContext();
        IQueryable<Models.ItemSale> q = db.ItemSales.AsNoTracking().Include(s => s.Item);
        if (from.HasValue) q = q.Where(s => (s.DateSold ?? s.CreatedAt) >= from.Value);
        if (to.HasValue) { var upper = to.Value.Date.AddDays(1); q = q.Where(s => (s.DateSold ?? s.CreatedAt) < upper); }

        var rows = await q
            .OrderByDescending(s => s.DateSold ?? s.CreatedAt)
            .Select(s => new ReportRow
            {
                ItemName = s.Item != null ? s.Item.Name : "(deleted item)",
                ItemType = s.Item != null ? s.Item.Type : "",
                ItemNumber = s.Item != null ? s.Item.ItemNumber : null,
                Date = s.DateSold ?? s.CreatedAt,
                Quantity = s.Quantity,
                UnitPrice = s.SalePrice,
                LineTotal = s.Quantity * s.SalePrice,
            })
            .ToListAsync();

        return new ReportResult { Rows = rows };
    }
}
