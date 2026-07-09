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
}
