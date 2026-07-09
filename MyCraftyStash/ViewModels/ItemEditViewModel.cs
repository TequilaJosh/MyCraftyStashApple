using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Add/edit form for one item. Init() with no id = add; with id = edit.</summary>
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
    }

    [ObservableProperty] public partial string PageTitle { get; set; } = "Add item";
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string Type { get; set; }
    [ObservableProperty] public partial string? Theme { get; set; }
    [ObservableProperty] public partial string? ItemNumber { get; set; }
    [ObservableProperty] public partial string? Location { get; set; }
    [ObservableProperty] public partial string? PurchasedFrom { get; set; }
    [ObservableProperty] public partial string? PriceText { get; set; }
    [ObservableProperty] public partial string? CurrentStockText { get; set; }
    [ObservableProperty] public partial bool IsDiscontinued { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public async void Init(int id)
    {
        _id = id;
        PageTitle = "Edit item";
        var item = await _service.GetItemAsync(id);
        if (item is not null)
        {
            Name = item.Name;
            Type = item.Type;
            Theme = item.Theme;
            ItemNumber = item.ItemNumber;
            Location = item.Location;
            PurchasedFrom = item.PurchasedFrom;
            PriceText = item.Price?.ToString("0.00");
            CurrentStockText = item.CurrentStock?.ToString();
            IsDiscontinued = item.IsDiscontinued;
            Notes = item.Notes;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Name is required."; return; }
        if (string.IsNullOrWhiteSpace(Type)) { ErrorMessage = "Type is required."; return; }

        var item = _id > 0 ? await _service.GetItemAsync(_id) ?? new Item() : new Item();
        item.Name = Name.Trim();
        item.Type = Type.Trim();
        item.Theme = Blank(Theme);
        item.ItemNumber = Blank(ItemNumber);
        item.Location = Blank(Location);
        item.PurchasedFrom = Blank(PurchasedFrom);
        item.Price = decimal.TryParse(PriceText, out var p) ? p : null;
        item.CurrentStock = int.TryParse(CurrentStockText, out var s) ? s : null;
        item.IsDiscontinued = IsDiscontinued;
        item.Notes = Blank(Notes);

        if (_id > 0) await _service.UpdateItemAsync(item);
        else await _service.AddItemAsync(item);

        _nav.Back();
    }

    [RelayCommand]
    private void Cancel() => _nav.Back();

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
