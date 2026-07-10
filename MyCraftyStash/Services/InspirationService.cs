using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>Aggregated stats for a board card (image + child-board counts, cover).</summary>
public class BoardStats
{
    public int ImageCount { get; set; }
    public int ChildBoardCount { get; set; }
    public string? CoverImageUrl { get; set; }
}

/// <summary>
/// Local inspiration data access: board hierarchy, images (with metadata +
/// board scoping + filters), and item links — cloning the desktop's
/// InspirationService surface over the shared inspiration_* tables.
/// </summary>
public class InspirationService
{
    // ── Images ────────────────────────────────────────────────────────────────

    /// <summary>All images (used by legacy flat gallery / fallback).</summary>
    public async Task<List<InspirationImage>> GetAllAsync()
    {
        using var db = new InventoryDbContext();
        return await db.InspirationImages.AsNoTracking()
            .OrderByDescending(i => i.CreatedAt).ToListAsync();
    }

    /// <summary>Images directly in a board (null = uncategorized root level).</summary>
    public async Task<List<InspirationImage>> GetImagesForBoardAsync(int? boardId)
    {
        using var db = new InventoryDbContext();
        return await db.InspirationImages.AsNoTracking()
            .Where(i => i.BoardId == boardId)
            .OrderByDescending(i => i.CreatedAt).ToListAsync();
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

    /// <summary>Updates the editable metadata for an image.</summary>
    public async Task UpdateAsync(InspirationImage image)
    {
        using var db = new InventoryDbContext();
        var e = await db.InspirationImages.FindAsync(image.Id);
        if (e is null) return;
        e.Title = image.Title;
        e.Notes = image.Notes;
        e.Color = image.Color;
        e.TeColor = image.TeColor;
        e.Types = image.Types;
        e.Theme = image.Theme;
        e.Sentiment = image.Sentiment;
        e.BoardId = image.BoardId;
        await db.SaveChangesAsync();
    }

    public async Task MoveImageToBoardAsync(int imageId, int? boardId)
    {
        using var db = new InventoryDbContext();
        var img = await db.InspirationImages.FindAsync(imageId);
        if (img is null) return;
        img.BoardId = boardId;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new InventoryDbContext();
        var image = await db.InspirationImages.FindAsync(id);
        if (image is null) return;
        var links = db.InspirationImageItems.Where(l => l.InspirationImageId == id);
        db.InspirationImageItems.RemoveRange(links);
        db.InspirationImages.Remove(image);
        await db.SaveChangesAsync();
    }

    // ── Item links ──────────────────────────────────────────────────────────

    public async Task<List<Item>> GetLinkedItemsAsync(int imageId)
    {
        using var db = new InventoryDbContext();
        return await db.InspirationImageItems.AsNoTracking()
            .Where(l => l.InspirationImageId == imageId)
            .Include(l => l.Item)
            .Select(l => l.Item)
            .ToListAsync();
    }

    /// <summary>Replaces the set of items linked to an image.</summary>
    public async Task SetLinkedItemsAsync(int imageId, IEnumerable<int> itemIds)
    {
        using var db = new InventoryDbContext();
        var old = db.InspirationImageItems.Where(l => l.InspirationImageId == imageId);
        db.InspirationImageItems.RemoveRange(old);
        foreach (var id in itemIds.Distinct())
            db.InspirationImageItems.Add(new InspirationImageItem { InspirationImageId = imageId, ItemId = id });
        await db.SaveChangesAsync();
    }

    /// <summary>Image ids whose linked items match a type and/or theme filter.</summary>
    public async Task<HashSet<int>> GetImageIdsByItemFilterAsync(string? type, string? theme)
    {
        using var db = new InventoryDbContext();
        var q = db.InspirationImageItems.AsNoTracking().Include(l => l.Item).AsQueryable();
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(l => l.Item.Type == type);
        if (!string.IsNullOrWhiteSpace(theme))
            q = q.Where(l => l.Item.Theme != null && l.Item.Theme.Contains(theme));
        return (await q.Select(l => l.InspirationImageId).Distinct().ToListAsync()).ToHashSet();
    }

    // ── Boards ────────────────────────────────────────────────────────────────

    /// <summary>Direct child boards of a parent (null = top level), ordered.</summary>
    public async Task<List<InspirationBoard>> GetBoardsAtLevelAsync(int? parentBoardId)
    {
        using var db = new InventoryDbContext();
        return await db.InspirationBoards.AsNoTracking()
            .Where(b => b.ParentBoardId == parentBoardId)
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<List<InspirationBoard>> GetAllBoardsFlatAsync()
    {
        using var db = new InventoryDbContext();
        return await db.InspirationBoards.AsNoTracking()
            .OrderBy(b => b.Name).ToListAsync();
    }

    public async Task<InspirationBoard?> GetBoardAsync(int id)
    {
        using var db = new InventoryDbContext();
        return await db.InspirationBoards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<BoardStats> GetBoardStatsAsync(int boardId)
    {
        using var db = new InventoryDbContext();
        var cover = await db.InspirationImages.AsNoTracking()
            .Where(i => i.BoardId == boardId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.ImageUrl).FirstOrDefaultAsync();
        return new BoardStats
        {
            ImageCount = await db.InspirationImages.CountAsync(i => i.BoardId == boardId),
            ChildBoardCount = await db.InspirationBoards.CountAsync(b => b.ParentBoardId == boardId),
            CoverImageUrl = cover,
        };
    }

    /// <summary>Breadcrumb path from root down to (and including) this board.</summary>
    public async Task<List<InspirationBoard>> GetBoardPathAsync(int boardId)
    {
        using var db = new InventoryDbContext();
        var all = await db.InspirationBoards.AsNoTracking().ToDictionaryAsync(b => b.Id);
        var path = new List<InspirationBoard>();
        int? cur = boardId;
        while (cur is int id && all.TryGetValue(id, out var b))
        {
            path.Insert(0, b);
            cur = b.ParentBoardId;
        }
        return path;
    }

    public async Task<int> CreateBoardAsync(InspirationBoard board)
    {
        using var db = new InventoryDbContext();
        board.CreatedAt = DateTime.Now;
        db.InspirationBoards.Add(board);
        await db.SaveChangesAsync();
        return board.Id;
    }

    public async Task UpdateBoardAsync(InspirationBoard board)
    {
        using var db = new InventoryDbContext();
        var e = await db.InspirationBoards.FindAsync(board.Id);
        if (e is null) return;
        e.Name = board.Name;
        e.Description = board.Description;
        e.DefaultTypes = board.DefaultTypes;
        e.DefaultThemes = board.DefaultThemes;
        e.DefaultColors = board.DefaultColors;
        e.DefaultSentiment = board.DefaultSentiment;
        e.DefaultTeColors = board.DefaultTeColors;
        e.DefaultSubtypes = board.DefaultSubtypes;
        e.DefaultItemIds = board.DefaultItemIds;
        await db.SaveChangesAsync();
    }

    /// <summary>Deletes a board, promoting its images and child boards to the
    /// parent level (desktop behavior).</summary>
    public async Task DeleteBoardAsync(int boardId)
    {
        using var db = new InventoryDbContext();
        var board = await db.InspirationBoards.FindAsync(boardId);
        if (board is null) return;
        int? parent = board.ParentBoardId;

        foreach (var img in db.InspirationImages.Where(i => i.BoardId == boardId))
            img.BoardId = parent;
        foreach (var child in db.InspirationBoards.Where(b => b.ParentBoardId == boardId))
            child.ParentBoardId = parent;

        db.InspirationBoards.Remove(board);
        await db.SaveChangesAsync();
    }

    // ── Taxonomy option sources ───────────────────────────────────────────────

    /// <summary>Distinct non-empty themes across inventory items.</summary>
    public async Task<List<string>> GetThemeOptionsAsync()
    {
        using var db = new InventoryDbContext();
        var raw = await db.Items.AsNoTracking()
            .Where(i => i.Theme != null && i.Theme != "")
            .Select(i => i.Theme!).ToListAsync();
        return raw.SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();
    }

    /// <summary>Distinct non-empty item types across inventory.</summary>
    public async Task<List<string>> GetTypeOptionsAsync()
    {
        using var db = new InventoryDbContext();
        return await db.Items.AsNoTracking()
            .Where(i => i.Type != "").Select(i => i.Type)
            .Distinct().OrderBy(t => t).ToListAsync();
    }

    /// <summary>Common inspiration color names (desktop reads these from config).</summary>
    public static IReadOnlyList<string> ColorOptions { get; } = new[]
    {
        "Red", "Orange", "Yellow", "Green", "Teal", "Blue", "Navy", "Purple",
        "Pink", "Magenta", "Brown", "Tan", "Cream", "White", "Gray", "Black",
        "Gold", "Silver", "Rose Gold", "Multi",
    };
}
