using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Add/edit form for one item. Init() with no id = add; Id passed = edit.</summary>
public partial class ItemEditViewModel : ObservableObject
{
    private readonly InventoryService _service;
    private readonly AppNavigator _nav;
    private int _id;

    public ItemEditViewModel(InventoryService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
        Name = "";
        Type = "";
        DatePurchased = DateTime.Today;
    }

    [ObservableProperty] public partial string PageTitle { get; set; } = "Add item";
    [ObservableProperty] public partial string Name { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStencilLayers))]
    public partial string Type { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStencilLayers))]
    public partial string? Subtype { get; set; }

    [ObservableProperty] public partial string? Theme { get; set; }
    [ObservableProperty] public partial string? ItemNumber { get; set; }
    [ObservableProperty] public partial string? Location { get; set; }
    [ObservableProperty] public partial string? PurchasedFrom { get; set; }
    [ObservableProperty] public partial string? PriceText { get; set; }
    [ObservableProperty] public partial string? CurrentStockText { get; set; }
    [ObservableProperty] public partial string? PackSizeText { get; set; }
    [ObservableProperty] public partial string? StencilLayersText { get; set; }
    [ObservableProperty] public partial string? SiteUrl { get; set; }
    [ObservableProperty] public partial DateTime DatePurchased { get; set; }
    [ObservableProperty] public partial bool HasDatePurchased { get; set; }
    [ObservableProperty] public partial bool IsDiscontinued { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }
    [ObservableProperty] public partial string? ImageUrl { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    /// <summary>Show the "number of layers" field when type or subtype mentions stencil.</summary>
    public bool ShowStencilLayers =>
        (Type?.Contains("stencil", StringComparison.OrdinalIgnoreCase) ?? false) ||
        (Subtype?.Contains("stencil", StringComparison.OrdinalIgnoreCase) ?? false);

    public async void Init(int id)
    {
        if (id <= 0) return;
        _id = id;
        PageTitle = "Edit item";
        var item = await _service.GetItemAsync(id);
        if (item is null) return;
        Name = item.Name;
        Type = item.Type;
        Subtype = item.Subtype;
        Theme = item.Theme;
        ItemNumber = item.ItemNumber;
        Location = item.Location;
        PurchasedFrom = item.PurchasedFrom;
        PriceText = item.Price?.ToString("0.00");
        CurrentStockText = item.CurrentStock?.ToString();
        PackSizeText = item.PackSize?.ToString();
        StencilLayersText = item.StencilLayers?.ToString();
        SiteUrl = item.SiteUrl;
        IsDiscontinued = item.IsDiscontinued;
        Notes = item.Notes;
        ImageUrl = item.ImageUrl;
        if (item.DatePurchased is { } dp) { DatePurchased = dp; HasDatePurchased = true; }
    }

    [RelayCommand]
    private async Task PickImage()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo is null) return;
            using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            ImageUrl = "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray());
        }
        catch { }
    }

    [RelayCommand]
    private void ClearImage() => ImageUrl = null;

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Name is required."; return; }
        if (string.IsNullOrWhiteSpace(Type)) { ErrorMessage = "Type is required."; return; }

        var item = _id > 0 ? await _service.GetItemAsync(_id) ?? new Item() : new Item();
        item.Name = Name.Trim();
        item.Type = Type.Trim();
        item.Subtype = Blank(Subtype);
        item.Theme = Blank(Theme);
        item.ItemNumber = Blank(ItemNumber);
        item.Location = Blank(Location);
        item.PurchasedFrom = Blank(PurchasedFrom);
        item.Price = decimal.TryParse(PriceText, out var pr) ? pr : null;
        item.CurrentStock = int.TryParse(CurrentStockText, out var st) ? st : null;
        item.PackSize = int.TryParse(PackSizeText, out var pk) ? pk : null;
        item.StencilLayers = ShowStencilLayers && int.TryParse(StencilLayersText, out var sl) ? sl : null;
        item.SiteUrl = Blank(SiteUrl);
        item.DatePurchased = HasDatePurchased ? DatePurchased : null;
        item.IsDiscontinued = IsDiscontinued;
        item.Notes = Blank(Notes);
        item.ImageUrl = ImageUrl;

        if (_id > 0) await _service.UpdateItemAsync(item);
        else await _service.AddItemAsync(item);

        _nav.Back();
    }

    [RelayCommand]
    private void Cancel() => _nav.Back();

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
