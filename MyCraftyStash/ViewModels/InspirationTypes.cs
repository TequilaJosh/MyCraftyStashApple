using CommunityToolkit.Mvvm.ComponentModel;

namespace MyCraftyStash.ViewModels;

/// <summary>A checkable option in a multi-select picker (color/theme/type/board).</summary>
public partial class CheckOption : ObservableObject
{
    public CheckOption(string label, object? tag = null) { Label = label; Tag = tag; }
    public string Label { get; }
    public object? Tag { get; }
    [ObservableProperty] public partial bool IsSelected { get; set; }
}

/// <summary>A board card in the boards strip (board + aggregated stats + labels).</summary>
public partial class BoardCard : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool HasDefaults { get; set; }
    [ObservableProperty] public partial string? CoverImageUrl { get; set; }
    [ObservableProperty] public partial string CountLabel { get; set; } = "No images";
    [ObservableProperty] public partial string ChildLabel { get; set; } = "";
    public bool HasChildren => !string.IsNullOrEmpty(ChildLabel);
}

/// <summary>An image card in the gallery (image + organize selection state).</summary>
public partial class ImageCard : ObservableObject
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = "";
    public string? Title { get; set; }
    public bool HasTitle => !string.IsNullOrEmpty(Title);
    [ObservableProperty] public partial bool IsOrgSelected { get; set; }
}

/// <summary>One breadcrumb hop (root has null BoardId).</summary>
public class BreadcrumbEntry
{
    public int? BoardId { get; set; }
    public string Name { get; set; } = "";
    public bool IsLast { get; set; }
    public bool ShowSeparator { get; set; }
}

/// <summary>A confirmed "items used" tag on the add form.</summary>
public class ItemTag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>A linked-item row shown in the image detail popup.</summary>
public class LinkedItemRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Meta { get; set; } = "";   // "Type · #Number"
}
