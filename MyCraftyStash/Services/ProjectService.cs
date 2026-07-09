using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>Local project data access over the shared projects schema.</summary>
public class ProjectService
{
    public async Task<List<Project>> GetAllAsync(string? search = null)
    {
        using var db = new InventoryDbContext();
        IQueryable<Project> query = db.Projects.AsNoTracking().Include(p => p.ProjectItems);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{s}%") ||
                (p.Technique != null && EF.Functions.Like(p.Technique, $"%{s}%")));
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    /// <summary>Full project with its linked inventory items (for the detail view).</summary>
    public async Task<Project?> GetAsync(int id)
    {
        using var db = new InventoryDbContext();
        return await db.Projects.AsNoTracking()
            .Include(p => p.ProjectItems).ThenInclude(pi => pi.Item)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int> AddAsync(Project project)
    {
        using var db = new InventoryDbContext();
        project.CreatedAt = DateTime.Now;
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project.Id;
    }

    public async Task UpdateAsync(Project project)
    {
        using var db = new InventoryDbContext();
        // Update scalar fields only; item links are managed separately.
        var existing = await db.Projects.FindAsync(project.Id);
        if (existing is null) return;
        existing.Name = project.Name;
        existing.Description = project.Description;
        existing.Technique = project.Technique;
        existing.Notes = project.Notes;
        existing.ImageUrl = project.ImageUrl;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new InventoryDbContext();
        var project = await db.Projects.FindAsync(id);
        if (project is not null)
        {
            db.Projects.Remove(project);
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> GetCountAsync()
    {
        using var db = new InventoryDbContext();
        return await db.Projects.CountAsync();
    }

    // ── Item links ───────────────────────────────────────────────────────────

    public async Task AddItemToProjectAsync(int projectId, int itemId)
    {
        using var db = new InventoryDbContext();
        bool exists = await db.ProjectItems.AnyAsync(pi => pi.ProjectId == projectId && pi.ItemId == itemId);
        if (exists) return;
        int nextOrder = await db.ProjectItems.Where(pi => pi.ProjectId == projectId)
            .Select(pi => (int?)pi.SortOrder).MaxAsync() ?? 0;
        db.ProjectItems.Add(new ProjectItem { ProjectId = projectId, ItemId = itemId, SortOrder = nextOrder + 1 });
        await db.SaveChangesAsync();
    }

    public async Task RemoveItemFromProjectAsync(int projectId, int itemId)
    {
        using var db = new InventoryDbContext();
        var link = await db.ProjectItems.FirstOrDefaultAsync(pi => pi.ProjectId == projectId && pi.ItemId == itemId);
        if (link is not null)
        {
            db.ProjectItems.Remove(link);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Inventory items not yet linked to this project, optionally filtered.</summary>
    public async Task<List<Item>> GetLinkableItemsAsync(int projectId, string? search = null)
    {
        using var db = new InventoryDbContext();
        var linked = db.ProjectItems.Where(pi => pi.ProjectId == projectId).Select(pi => pi.ItemId);
        IQueryable<Item> query = db.Items.AsNoTracking().Where(i => !linked.Contains(i.Id));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(i => EF.Functions.Like(i.Name, $"%{s}%") ||
                                     (i.Type != null && EF.Functions.Like(i.Type, $"%{s}%")));
        }
        return await query.OrderBy(i => i.Name).ToListAsync();
    }
}
