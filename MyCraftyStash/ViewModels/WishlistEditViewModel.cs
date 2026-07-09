using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Add/edit form for a wish-list item. Init(0) = add; Init(id) = edit.</summary>
public partial class WishlistEditViewModel : ObservableObject
{
    private readonly WishlistService _service;
    private readonly AppNavigator _nav;
    private int _id;

    public WishlistEditViewModel(WishlistService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
        Name = "";
        Priority = "Low";
    }

    public List<string> Priorities { get; } = new() { "Low", "Medium", "High" };

    [ObservableProperty] public partial string PageTitle { get; set; } = "Add to wish list";
    [ObservableProperty] public partial bool IsEditing { get; set; }
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string? Type { get; set; }
    [ObservableProperty] public partial string? Theme { get; set; }
    [ObservableProperty] public partial string? ItemNumber { get; set; }
    [ObservableProperty] public partial string? PriceText { get; set; }
    [ObservableProperty] public partial string Priority { get; set; }
    [ObservableProperty] public partial string? PurchasedFrom { get; set; }
    [ObservableProperty] public partial string? Url { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public async void Init(int id)
    {
        if (id <= 0) return;
        _id = id;
        IsEditing = true;
        PageTitle = "Edit wish-list item";
        var item = await _service.GetAsync(id);
        if (item is not null)
        {
            Name = item.Name;
            Type = item.Type;
            Theme = item.Theme;
            ItemNumber = item.ItemNumber;
            PriceText = item.Price?.ToString("0.00");
            Priority = item.PriorityLabel;
            PurchasedFrom = item.PurchasedFrom;
            Url = item.Url;
            Notes = item.Notes;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Name is required."; return; }

        var item = _id > 0 ? await _service.GetAsync(_id) ?? new WishlistItem() : new WishlistItem();
        item.Name = Name.Trim();
        item.Type = Blank(Type);
        item.Theme = Blank(Theme);
        item.ItemNumber = Blank(ItemNumber);
        item.Price = decimal.TryParse(PriceText, out var p) ? p : null;
        item.Priority = Priority switch { "High" => 3, "Medium" => 2, _ => 1 };
        item.PurchasedFrom = Blank(PurchasedFrom);
        item.Url = Blank(Url);
        item.Notes = Blank(Notes);

        if (_id > 0) await _service.UpdateAsync(item);
        else await _service.AddAsync(item);

        _nav.Back();
    }

    [RelayCommand]
    private void Cancel() => _nav.Back();

    [RelayCommand]
    private async Task Delete()
    {
        if (_id <= 0) return;
        var page = Application.Current?.Windows[0].Page;
        bool ok = page is not null && await page.DisplayAlert("Remove item",
            $"Remove \"{Name}\" from your wish list?", "Remove", "Cancel");
        if (!ok) return;

        await _service.DeleteAsync(_id);
        _nav.Back();
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
