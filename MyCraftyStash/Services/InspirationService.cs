using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>Local inspiration-gallery data access over inspiration_images.</summary>
public class InspirationService
{
    public async Task<List<InspirationImage>> GetAllAsync()
    {
        using var db = new InventoryDbContext();
        return await db.InspirationImages.AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<InspirationImage?> GetAsync(int id)
    {
        using var db = new InventoryDbContext();
        return await db.InspirationImages.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<int> AddAsync(InspirationImage image)
    {
        using var db = new InventoryDbContext();
        image.CreatedAt = DateTime.Now;
        db.InspirationImages.Add(image);
        await db.SaveChangesAsync();
        return image.Id;
    }

    public async Task UpdateAsync(InspirationImage image)
    {
        using var db = new InventoryDbContext();
        var existing = await db.InspirationImages.FindAsync(image.Id);
        if (existing is null) return;
        existing.Title = image.Title;
        existing.Notes = image.Notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new InventoryDbContext();
        var image = await db.InspirationImages.FindAsync(id);
        if (image is not null)
        {
            db.InspirationImages.Remove(image);
            await db.SaveChangesAsync();
        }
    }
}
