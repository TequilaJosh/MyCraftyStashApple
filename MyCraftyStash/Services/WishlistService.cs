using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>
/// Local wish-list data access, backed by the shared SQLite schema
/// (wishlist_items). A lean subset of the desktop service — list/get/add/
/// update/delete — matching its priority-descending ordering.
/// </summary>
public class WishlistService
{
    public async Task<List<WishlistItem>> GetAllAsync(string? search = null)
    {
        using var db = new InventoryDbContext();
        IQueryable<WishlistItem> query = db.WishlistItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(w =>
                EF.Functions.Like(w.Name, $"%{s}%") ||
                (w.Type != null && EF.Functions.Like(w.Type, $"%{s}%")) ||
                (w.Theme != null && EF.Functions.Like(w.Theme, $"%{s}%")));
        }

        return await query
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.Name)
            .ToListAsync();
    }

    public async Task<WishlistItem?> GetAsync(int id)
    {
        using var db = new InventoryDbContext();
        return await db.WishlistItems.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<int> AddAsync(WishlistItem item)
    {
        using var db = new InventoryDbContext();
        item.CreatedAt = DateTime.Now;
        db.WishlistItems.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    public async Task UpdateAsync(WishlistItem item)
    {
        using var db = new InventoryDbContext();
        db.WishlistItems.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task<int> GetCountAsync()
    {
        using var db = new InventoryDbContext();
        return await db.WishlistItems.CountAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new InventoryDbContext();
        var item = await db.WishlistItems.FindAsync(id);
        if (item is not null)
        {
            db.WishlistItems.Remove(item);
            await db.SaveChangesAsync();
        }
    }
}
