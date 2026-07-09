using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Read view for one item, with Edit and Delete.</summary>
public partial class ItemDetailViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InventoryService _service;
    private readonly AppNavigator _nav;
    private int _id;

    public ItemDetailViewModel(InventoryService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
    }

    [ObservableProperty] public partial Item? Item { get; set; }

    public async void Init(int id)
    {
        _id = id;
        await Refresh();
    }

    public async Task Refresh()
    {
        if (_id > 0)
            Item = await _service.GetItemAsync(_id);
    }

    [RelayCommand]
    private void Edit()
    {
        if (Item is not null)
            _nav.PushEditItem(Item.Id);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (Item is null) return;
        var page = Application.Current?.Windows[0].Page;
        bool ok = page is not null && await page.DisplayAlert("Delete item",
            $"Delete \"{Item.Name}\"? This can't be undone.", "Delete", "Cancel");
        if (!ok) return;

        await _service.DeleteItemAsync(Item.Id);
        _nav.Back(); // back to the list (which reloads on return)
    }

    [RelayCommand]
    private void Back() => _nav.Back();
}
