using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Add/edit form for one item. No Id passed = add; Id passed = edit.</summary>
public partial class ItemEditViewModel : ObservableObject, IQueryAttributable
{
    private readonly InventoryService _service;
    private int _id;

    public ItemEditViewModel(InventoryService service)
    {
        _service = service;
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

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Id", out var value) && value is int id && id > 0)
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
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Name is required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Type))
        {
            ErrorMessage = "Type is required.";
            return;
        }

        // Load the existing row (edit) or start a new one (add) so we never
        // clobber fields the form doesn't expose yet.
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

        if (_id > 0)
            await _service.UpdateItemAsync(item);
        else
            await _service.AddItemAsync(item);

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private Task Cancel() => Shell.Current.GoToAsync("..");

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
