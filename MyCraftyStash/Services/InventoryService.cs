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

    /// <param name="searchMode">null = name/theme/number/sentiment; "name" = name only; "theme" = theme only.</param>
    public async Task<List<Item>> GetItemsAsync(string? search = null, string? type = null,
        string? searchMode = null, bool discontinuedOnly = false)
    {
        using var db = new InventoryDbContext();
        IQueryable<Item> query = db.Items.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type))
            query = query.Where(i => i.Type == type);

        if (discontinuedOnly)
            query = query.Where(i => i.IsDiscontinued);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = searchMode switch
            {
                "name" => query.Where(i => EF.Functions.Like(i.Name, $"%{s}%")),
                "theme" => query.Where(i => i.Theme != null && EF.Functions.Like(i.Theme, $"%{s}%")),
                _ => query.Where(i =>
                    EF.Functions.Like(i.Name, $"%{s}%") ||
                    (i.Theme != null && EF.Functions.Like(i.Theme, $"%{s}%")) ||
                    (i.ItemNumber != null && EF.Functions.Like(i.ItemNumber, $"%{s}%")) ||
                    (i.Sentiments != null && EF.Functions.Like(i.Sentiments, $"%{s}%"))),
            };
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

    /// <summary>Count of items running low (stock set and at/below 5).</summary>
    public async Task<int> GetLowStockCountAsync()
    {
        using var db = new InventoryDbContext();
        return await db.Items.CountAsync(i => i.CurrentStock != null && i.CurrentStock <= 5);
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

    // ── Item images (gallery) ────────────────────────────────────────────────
    public async Task<List<ItemImage>> GetItemImagesAsync(int itemId)
    {
        using var db = new InventoryDbContext();
        return await db.ItemImages.AsNoTracking()
            .Where(x => x.ItemId == itemId).OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .ToListAsync();
    }

    public async Task AddItemImageAsync(int itemId, string imageUrl)
    {
        using var db = new InventoryDbContext();
        int next = await db.ItemImages.Where(x => x.ItemId == itemId)
            .Select(x => (int?)x.SortOrder).MaxAsync() ?? 0;
        db.ItemImages.Add(new ItemImage { ItemId = itemId, ImageUrl = imageUrl, SortOrder = next + 1 });
        await db.SaveChangesAsync();
    }

    public async Task DeleteItemImageAsync(int imageId)
    {
        using var db = new InventoryDbContext();
        var img = await db.ItemImages.FindAsync(imageId);
        if (img is not null) { db.ItemImages.Remove(img); await db.SaveChangesAsync(); }
    }

    // ── Purchase history ─────────────────────────────────────────────────────
    public async Task<List<ItemPurchase>> GetPurchasesAsync(int itemId)
    {
        using var db = new InventoryDbContext();
        return await db.ItemPurchases.AsNoTracking()
            .Where(p => p.ItemId == itemId)
            .OrderByDescending(p => p.DatePurchased ?? p.CreatedAt).ToListAsync();
    }

    public async Task AddPurchaseAsync(int itemId, int qty, decimal pricePerItem, DateTime? date)
    {
        using var db = new InventoryDbContext();
        db.ItemPurchases.Add(new ItemPurchase { ItemId = itemId, Quantity = qty, PricePerItem = pricePerItem, DatePurchased = date });
        await db.SaveChangesAsync();
    }

    public async Task DeletePurchaseAsync(int purchaseId)
    {
        using var db = new InventoryDbContext();
        var p = await db.ItemPurchases.FindAsync(purchaseId);
        if (p is not null) { db.ItemPurchases.Remove(p); await db.SaveChangesAsync(); }
    }

    // ── Sales history ────────────────────────────────────────────────────────
    public async Task<List<ItemSale>> GetSalesAsync(int itemId)
    {
        using var db = new InventoryDbContext();
        return await db.ItemSales.AsNoTracking()
            .Where(s => s.ItemId == itemId)
            .OrderByDescending(s => s.DateSold ?? s.CreatedAt).ToListAsync();
    }

    public async Task AddSaleAsync(int itemId, int qty, decimal salePrice, DateTime? date)
    {
        using var db = new InventoryDbContext();
        db.ItemSales.Add(new ItemSale { ItemId = itemId, Quantity = qty, SalePrice = salePrice, DateSold = date });
        await db.SaveChangesAsync();
    }

    public async Task DeleteSaleAsync(int saleId)
    {
        using var db = new InventoryDbContext();
        var s = await db.ItemSales.FindAsync(saleId);
        if (s is not null) { db.ItemSales.Remove(s); await db.SaveChangesAsync(); }
    }

    // ── Related items ────────────────────────────────────────────────────────
    public async Task<List<Item>> GetRelatedItemsAsync(int itemId)
    {
        using var db = new InventoryDbContext();
        var ids = await db.ItemRelationships.AsNoTracking()
            .Where(r => r.ItemId == itemId).Select(r => r.RelatedItemId).ToListAsync();
        return await db.Items.AsNoTracking().Where(i => ids.Contains(i.Id))
            .OrderBy(i => i.Name).ToListAsync();
    }

    public async Task AddRelatedItemAsync(int itemId, int relatedId)
    {
        if (itemId == relatedId) return;
        using var db = new InventoryDbContext();
        bool exists = await db.ItemRelationships.AnyAsync(r => r.ItemId == itemId && r.RelatedItemId == relatedId);
        if (exists) return;
        // Store both directions so the link shows on either item.
        db.ItemRelationships.Add(new ItemRelationship { ItemId = itemId, RelatedItemId = relatedId });
        if (!await db.ItemRelationships.AnyAsync(r => r.ItemId == relatedId && r.RelatedItemId == itemId))
            db.ItemRelationships.Add(new ItemRelationship { ItemId = relatedId, RelatedItemId = itemId });
        await db.SaveChangesAsync();
    }

    public async Task RemoveRelatedItemAsync(int itemId, int relatedId)
    {
        using var db = new InventoryDbContext();
        var links = db.ItemRelationships.Where(r =>
            (r.ItemId == itemId && r.RelatedItemId == relatedId) ||
            (r.ItemId == relatedId && r.RelatedItemId == itemId));
        db.ItemRelationships.RemoveRange(links);
        await db.SaveChangesAsync();
    }

    // ── Sentiments ───────────────────────────────────────────────────────────
    public async Task<List<SentimentImage>> GetSentimentsAsync(int itemId)
    {
        using var db = new InventoryDbContext();
        return await db.SentimentImages.AsNoTracking()
            .Where(s => s.ItemId == itemId).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync();
    }

    public async Task AddSentimentAsync(int itemId, string text, string? imageData = null)
    {
        using var db = new InventoryDbContext();
        int next = await db.SentimentImages.Where(s => s.ItemId == itemId)
            .Select(s => (int?)s.SortOrder).MaxAsync() ?? 0;
        db.SentimentImages.Add(new SentimentImage
        {
            ItemId = itemId,
            ExtractedText = text,
            SearchText = text.ToLowerInvariant(),
            ImageData = imageData ?? string.Empty,
            SortOrder = next + 1,
        });
        await db.SaveChangesAsync();
    }

    public async Task DeleteSentimentAsync(int sentimentId)
    {
        using var db = new InventoryDbContext();
        var s = await db.SentimentImages.FindAsync(sentimentId);
        if (s is not null) { db.SentimentImages.Remove(s); await db.SaveChangesAsync(); }
    }

    // ── Subtypes for a type ──────────────────────────────────────────────────
    /// <summary>Distinct subtype tokens seen on items of a given type. (The
    /// desktop reads these from a config file; here we derive them from data
    /// until a settings-backed list is added.)</summary>
    public async Task<List<string>> GetSubtypesForTypeAsync(string type)
    {
        using var db = new InventoryDbContext();
        var raw = await db.Items.AsNoTracking()
            .Where(i => i.Type == type && i.Subtype != null && i.Subtype != "")
            .Select(i => i.Subtype!).ToListAsync();
        return raw.SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
    }
}
