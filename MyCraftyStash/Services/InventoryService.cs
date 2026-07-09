using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>
/// Local inventory data access, backed by the same SQLite schema the Windows
/// desktop app uses (via the shared <see cref="InventoryDbContext"/>). This is
/// a lean, MAUI-focused subset of the desktop service — the features the app's
/// screens need today — not a line-for-line port of the 2,700-line original.
/// </summary>
public class InventoryService
{
    /// <summary>Creates the database and schema on first run. Idempotent.</summary>
    public async Task InitializeAsync()
    {
        using var db = new InventoryDbContext();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task<List<Item>> GetItemsAsync(string? search = null, string? type = null)
    {
        using var db = new InventoryDbContext();
        IQueryable<Item> query = db.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(i => i.Type == type);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(i =>
                EF.Functions.Like(i.Name, $"%{s}%") ||
                (i.Theme != null && EF.Functions.Like(i.Theme, $"%{s}%")) ||
                (i.ItemNumber != null && EF.Functions.Like(i.ItemNumber, $"%{s}%")) ||
                (i.Sentiments != null && EF.Functions.Like(i.Sentiments, $"%{s}%")));
        }

        return await query.OrderBy(i => i.Name).ToListAsync();
    }

    public async Task<Item?> GetItemAsync(int id)
    {
        using var db = new InventoryDbContext();
        return await db.Items
            .AsNoTracking()
            .Include(i => i.Images)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    /// <summary>Distinct types already used, for the add/edit type suggestions.</summary>
    public async Task<List<string>> GetTypesAsync()
    {
        using var db = new InventoryDbContext();
        return await db.Items
            .AsNoTracking()
            .Where(i => i.Type != "")
            .Select(i => i.Type)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<int> AddItemAsync(Item item)
    {
        using var db = new InventoryDbContext();
        item.CreatedAt = DateTime.Now;
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task UpdateItemAsync(Item item)
    {
        using var db = new InventoryDbContext();
        db.Items.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteItemAsync(int id)
    {
        using var db = new InventoryDbContext();
        var item = await db.Items.FindAsync(id);
        if (item is not null)
        {
            db.Items.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> GetItemCountAsync()
    {
        using var db = new InventoryDbContext();
        return await db.Items.CountAsync();
    }

    /// <summary>Items whose sentiment text matches — powers Sentiment Search.</summary>
    public async Task<List<Item>> SearchBySentimentAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<Item>();

        using var db = new InventoryDbContext();
        var q = query.Trim();
        return await db.Items.AsNoTracking()
            .Where(i => i.Sentiments != null && EF.Functions.Like(i.Sentiments, $"%{q}%"))
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    /// <summary>Items that have a stock count, lowest first — the "running low"
    /// view for the Stock Tracker.</summary>
    public async Task<List<Item>> GetStockItemsAsync()
    {
        using var db = new InventoryDbContext();
        return await db.Items.AsNoTracking()
            .Where(i => i.CurrentStock != null)
            .OrderBy(i => i.CurrentStock)
            .ThenBy(i => i.Name)
            .ToListAsync();
    }
}
