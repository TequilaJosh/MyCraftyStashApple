using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Read view for one item, with Edit and Delete. Reloads on appear so
/// edits made on the edit page are reflected when navigating back.</summary>
public partial class ItemDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly InventoryService _service;

    public ItemDetailViewModel(InventoryService service)
    {
        _service = service;
    }

    [ObservableProperty] public partial Item? Item { get; set; }
    private int _id;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Id", out var value) && value is int id)
            _id = id;
    }

    [RelayCommand]
    public async Task Load()
    {
        if (_id > 0)
            Item = await _service.GetItemAsync(_id);
    }

    [RelayCommand]
    private async Task Edit()
    {
        if (Item is not null)
            await Shell.Current.GoToAsync("itemedit", new Dictionary<string, object> { ["Id"] = Item.Id });
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (Item is null) return;
        var page = Shell.Current.CurrentPage;
        bool ok = await page.DisplayAlert("Delete item",
            $"Delete \"{Item.Name}\"? This can't be undone.", "Delete", "Cancel");
        if (!ok) return;

        await _service.DeleteItemAsync(Item.Id);
        await Shell.Current.GoToAsync(".."); // back to the list
    }
}
