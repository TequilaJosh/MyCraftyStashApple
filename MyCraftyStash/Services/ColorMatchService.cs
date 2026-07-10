using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>
/// TE color-match chart access: the DMC/OLO → Taylored Expressions colour
/// mappings plus the two ownership signals (do you own the TE colour product,
/// and do you own the matching external supply). Clones the desktop service.
/// </summary>
public class ColorMatchService
{
    public const string SystemDmc = "DMC";
    public const string SystemOlo = "OLO";

    public async Task<List<ColorMatch>> GetAllAsync(string system)
    {
        using var db = new InventoryDbContext();
        return await db.ColorMatches.AsNoTracking()
            .Where(m => m.System == system)
            .OrderBy(m => m.TeColorName)
            .ToListAsync();
    }

    /// <summary>TE colours the user owns — inventory items in a coloured-product
    /// type whose Name matches a TE colour name.</summary>
    public async Task<HashSet<string>> GetOwnedTeColorNamesAsync()
    {
        var coloredTypes = new[]
        {
            "Ink - Full Size", "Ink - Mini", "Ink - Refill",
            "Watercolor", "Cardstock",
            "A2 Envelopes", "A7 Envelopes", "Mini Slim Envelopes",
        };
        using var db = new InventoryDbContext();
        var names = await db.Items.AsNoTracking()
            .Where(i => coloredTypes.Contains(i.Type))
            .Select(i => i.Name).ToListAsync();
        var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in names)
            if (!string.IsNullOrWhiteSpace(n)) owned.Add(n.Trim());
        return owned;
    }

    /// <summary>External-code strings (ItemNumber/Name) for items relevant to a
    /// system (Type/Name contains "DMC"/"OLO").</summary>
    public async Task<List<string>> GetOwnedExternalCodeStringsAsync(string system)
    {
        using var db = new InventoryDbContext();
        var rows = await db.Items.AsNoTracking()
            .Select(i => new { i.Type, i.Name, i.ItemNumber }).ToListAsync();
        var owned = new List<string>();
        foreach (var r in rows)
        {
            var t = r.Type ?? "";
            var n = r.Name ?? "";
            bool relevant = system switch
            {
                SystemOlo => t.Contains("OLO", StringComparison.OrdinalIgnoreCase) || n.Contains("OLO", StringComparison.OrdinalIgnoreCase),
                SystemDmc => t.Contains("DMC", StringComparison.OrdinalIgnoreCase) || n.Contains("DMC", StringComparison.OrdinalIgnoreCase),
                _ => false,
            };
            if (!relevant) continue;
            if (!string.IsNullOrWhiteSpace(r.ItemNumber)) owned.Add(r.ItemNumber.Trim());
            if (!string.IsNullOrWhiteSpace(n)) owned.Add(n.Trim());
        }
        return owned;
    }

    /// <summary>True when <paramref name="code"/> appears as a word-bounded token
    /// inside any owned string ("B-RV1.3" matches chart code "RV1.3", but "K"
    /// does not match "OREO"). Mirrors the desktop's boundary matching.</summary>
    public static bool ExternalOwned(string code, IEnumerable<string> ownedStrings)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        foreach (var s in ownedStrings)
        {
            int idx = s.IndexOf(code, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                bool leftOk = idx == 0 || !char.IsLetterOrDigit(s[idx - 1]);
                int end = idx + code.Length;
                bool rightOk = end >= s.Length || !char.IsLetterOrDigit(s[end]);
                if (leftOk && rightOk) return true;
                idx = s.IndexOf(code, idx + 1, StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }
}
