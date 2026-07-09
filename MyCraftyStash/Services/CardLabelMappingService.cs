using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;

namespace MyCraftyStash.Services;

/// <summary>One label's mapping to inventory: which Type (contains-match),
/// which subtype tokens, and an optional name-contains filter.</summary>
public class CardLabelMapping
{
    public string Label { get; set; } = string.Empty;
    public string? Type { get; set; }
    public List<string> Subtypes { get; set; } = new();
    public string? NameContains { get; set; }
}

/// <summary>
/// Maps the card build wizard's picker labels to inventory queries. Ported
/// verbatim from the desktop's CardLabelMappingService defaults so the wizard
/// pickers fill with the same items. (The desktop lets the user override the
/// map via config; here the defaults are used directly.)
/// </summary>
public class CardLabelMappingService
{
    public static readonly string[] CanonicalLabelOrder =
    {
        "Cardstock", "Foil Cardstock", "Glitter Cardstock", "Insider Cardstock",
        "Foil-It Cardstock", "Frames Die", "Stamps", "Dies", "Stencils",
        "Embellishments", "Stacklets", "Embossing Folders", "OLO Markers",
        "Watercolor", "Envelopes", "Foils", "Mini Cube Inks", "Full Pad Inks",
        "Embossing Powder", "Storage Bags", "Adhesives", "Glue Adhesive",
        "Foam Adhesive", "Tape Runner Adhesive", "All Planned Out",
        "Happy Medium", "Astro Paste", "Glitter",
    };

    private static readonly List<CardLabelMapping> Defaults = new()
    {
        new() { Label = "Cardstock", Type = "Cardstock", Subtypes = { "8.5x11" } },
        new() { Label = "Foil Cardstock", Type = "Cardstock", Subtypes = { "Foil" } },
        new() { Label = "Glitter Cardstock", Type = "Cardstock", Subtypes = { "Glitter" } },
        new() { Label = "Insider Cardstock", Type = "Cardstock", Subtypes = { "Insider" } },
        new() { Label = "Foil-It Cardstock", Type = "Cardstock", Subtypes = { "Foil-it" } },
        new() { Label = "Frames Die", Type = "Dies", Subtypes = { "Frames" } },
        new() { Label = "Stamps", Type = "Stamps" },
        new() { Label = "Dies", Type = "Dies" },
        new() { Label = "Stencils", Type = "Stencils" },
        new() { Label = "Embellishments", Type = "Embellishments" },
        new() { Label = "Stacklets", Type = "Stacklets" },
        new() { Label = "Embossing Folders", Type = "Embossing Folders" },
        new() { Label = "OLO Markers", Type = "OLO Markers" },
        new() { Label = "Watercolor", Type = "Watercolor" },
        new() { Label = "Envelopes", Type = "Envelopes" },
        new() { Label = "Foils", Type = "Foils" },
        new() { Label = "Mini Cube Inks", Subtypes = { "Mini Cube" } },
        new() { Label = "Full Pad Inks", Subtypes = { "Full Pad" } },
        new() { Label = "Embossing Powder", Subtypes = { "Embossing Powder" } },
        new() { Label = "Storage Bags", Type = "Storage Bags" },
        new() { Label = "Adhesives", Type = "Adhesive" },
        new() { Label = "Glue Adhesive", Type = "Adhesive", Subtypes = { "Glue" } },
        new() { Label = "Foam Adhesive", Type = "Adhesive", Subtypes = { "Foam" } },
        new() { Label = "Tape Runner Adhesive", Type = "Adhesive", Subtypes = { "Tape Runner" } },
        new() { Label = "All Planned Out", NameContains = "all planned out" },
        new() { Label = "Happy Medium", NameContains = "happy medium" },
        new() { Label = "Astro Paste", NameContains = "astro paste" },
        new() { Label = "Glitter", NameContains = "glitter" },
    };

    public CardLabelMapping? GetMapping(string label) =>
        Defaults.FirstOrDefault(m => string.Equals(m.Label, label, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Items eligible for a wizard label: case-insensitive Type contains-match,
    /// optional name-contains, then an exact subtype token post-filter —
    /// matching the desktop's GetWizardItemsAsync behavior.
    /// </summary>
    public async Task<List<WizardItemOption>> GetItemsForLabelAsync(string label)
    {
        var map = GetMapping(label);
        if (map is null) return new List<WizardItemOption>();

        using var db = new InventoryDbContext();
        var query = db.Items.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(map.Type))
        {
            var t = map.Type.ToLower();
            query = query.Where(i => i.Type.ToLower().Contains(t));
        }
        if (!string.IsNullOrEmpty(map.NameContains))
        {
            var n = map.NameContains.ToLower();
            query = query.Where(i => i.Name.ToLower().Contains(n));
        }

        var rows = await query.OrderBy(i => i.Name)
            .Select(i => new { i.Id, i.Name, i.Type, i.Subtype, i.StencilLayers, i.ImageUrl })
            .ToListAsync();

        // Exact subtype token post-filter (comma-separated tokens on the item).
        IEnumerable<dynamic> filtered = rows;
        if (map.Subtypes.Count > 0)
        {
            filtered = rows.Where(r =>
                r.Subtype != null &&
                ((string)r.Subtype).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(tok => map.Subtypes.Any(want => string.Equals(tok, want, StringComparison.OrdinalIgnoreCase))));
        }

        return filtered.Select(r => new WizardItemOption
        {
            Id = r.Id,
            Name = r.Name,
            ItemType = r.Type,
            Subtype = r.Subtype,
            StencilLayers = r.StencilLayers,
            ImageUrl = r.ImageUrl,
        }).ToList();
    }

    /// <summary>Distinct subtype tokens across a label's item pool ("All" first),
    /// for pickers that show a subtype filter strip.</summary>
    public static List<string> SubtypeTokens(IEnumerable<WizardItemOption> items)
    {
        var tokens = items.Where(i => !string.IsNullOrWhiteSpace(i.Subtype))
            .SelectMany(i => i.Subtype!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();
        tokens.Insert(0, "All");
        return tokens;
    }
}
