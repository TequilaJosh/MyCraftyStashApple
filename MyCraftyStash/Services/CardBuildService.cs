using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>
/// Loads and saves a project's card build (project_card_builds +
/// project_card_build_steps). One build per project; saving replaces the step
/// rows wholesale so ordering/edits are always consistent.
/// </summary>
public class CardBuildService
{
    /// <summary>The project's build with its ordered steps (steps include the
    /// linked inventory Item), or null if none exists yet.</summary>
    public async Task<ProjectCardBuild?> GetForProjectAsync(int projectId)
    {
        using var db = new InventoryDbContext();
        var build = await db.ProjectCardBuilds.AsNoTracking()
            .FirstOrDefaultAsync(b => b.ProjectId == projectId);
        if (build is null) return null;

        build.Steps = await db.ProjectCardBuildSteps.AsNoTracking()
            .Where(s => s.BuildId == build.Id)
            .Include(s => s.Item)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();
        return build;
    }

    /// <summary>Upserts the build header and rewrites all step rows.</summary>
    public async Task SaveAsync(int projectId, string cardBaseType, string? stateSnapshot, IList<ProjectCardBuildStep> steps)
    {
        using var db = new InventoryDbContext();

        var build = await db.ProjectCardBuilds.FirstOrDefaultAsync(b => b.ProjectId == projectId);
        if (build is null)
        {
            build = new ProjectCardBuild { ProjectId = projectId, CreatedAt = DateTime.Now };
            db.ProjectCardBuilds.Add(build);
        }
        build.CardBaseType = cardBaseType;
        build.StateSnapshot = stateSnapshot;
        await db.SaveChangesAsync(); // ensures build.Id

        // Replace all steps.
        var old = db.ProjectCardBuildSteps.Where(s => s.BuildId == build.Id);
        db.ProjectCardBuildSteps.RemoveRange(old);
        await db.SaveChangesAsync();

        int order = 1;
        foreach (var s in steps)
        {
            db.ProjectCardBuildSteps.Add(new ProjectCardBuildStep
            {
                BuildId = build.Id,
                StepOrder = order++,
                Section = s.Section,
                StepType = s.StepType,
                MatLayer = s.MatLayer,
                ItemId = s.ItemId,
                StackletDieId = s.StackletDieId,
                CuttingMethod = s.CuttingMethod,
                Label = s.Label,
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task DeleteForProjectAsync(int projectId)
    {
        using var db = new InventoryDbContext();
        var build = await db.ProjectCardBuilds.FirstOrDefaultAsync(b => b.ProjectId == projectId);
        if (build is null) return;
        var steps = db.ProjectCardBuildSteps.Where(s => s.BuildId == build.Id);
        db.ProjectCardBuildSteps.RemoveRange(steps);
        db.ProjectCardBuilds.Remove(build);
        await db.SaveChangesAsync();
    }

    public async Task<bool> HasBuildAsync(int projectId)
    {
        using var db = new InventoryDbContext();
        return await db.ProjectCardBuilds.AnyAsync(b => b.ProjectId == projectId);
    }
}
