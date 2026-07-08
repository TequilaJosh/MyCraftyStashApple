namespace MyCraftyStash.Models;

/// <summary>
/// One inventory item as returned by GET /api/items. Mirrors the cloud API
/// JSON (camelCase; deserialized case-insensitively). Dates stay ISO-8601
/// strings until something needs to do math on them.
/// </summary>
public class StashItem
{
    public Guid Id { get; set; }
    public int LocalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Subtype { get; set; }
    public string? Theme { get; set; }
    public string[] Sentiments { get; set; } = Array.Empty<string>();
    public string? ItemNumber { get; set; }
    public decimal? Price { get; set; }
    public string? DatePurchased { get; set; }
    public bool IsDiscontinued { get; set; }
    public int? StencilLayers { get; set; }
    public int? PackSize { get; set; }
    public int? CurrentStock { get; set; }
    public string? PurchasedFrom { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    /// <summary>15-minute SAS URL minted per response; null when no photo was uploaded.</summary>
    public string? ImageUrl { get; set; }
    public string? ThumbUrl { get; set; }
    public string? UploadedAt { get; set; }
    public string? UpdatedAt { get; set; }

    public string PriceDisplay => Price is { } p ? $"${p:0.00}" : string.Empty;
    public bool HasSentiments => Sentiments.Length > 0;
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
}

/// <summary>Envelope for the paginated /api/items response.</summary>
public class ItemsPage
{
    public List<StashItem> Items { get; set; } = new();
    public int Page { get; set; }
    public int PerPage { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

/// <summary>Subset of /api/whoami we care about (validates an API key).</summary>
public class Whoami
{
    public string UserId { get; set; } = string.Empty;
    public string? UserDetails { get; set; }
    public string? FirstName { get; set; }
}
