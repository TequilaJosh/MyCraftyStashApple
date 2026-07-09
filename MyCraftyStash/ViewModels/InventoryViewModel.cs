using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>The inventory grid: local search + card list, with add/open.</summary>
public partial class InventoryViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InventoryService _service;
    private readonly AppNavigator _nav;

    public InventoryViewModel(InventoryService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
        SearchText = string.Empty;
    }

    public ObservableCollection<Item> Items { get; } = new();

    [ObservableProperty] public partial string SearchText { get; set; }
    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }

    public Task Refresh() => Load();

    [RelayCommand]
    public async Task Load()
    {
        Busy = true;
        try
        {
            var items = await _service.GetItemsAsync(SearchText);
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);
        }
        finally
        {
            Busy = false;
            IsRefreshing = false;
            IsEmpty = Items.Count == 0;
        }
    }

    [RelayCommand]
    private Task DoRefresh() => Load();

    [RelayCommand]
    private Task Search() => Load();

    [RelayCommand]
    private void AddItem() => _nav.PushAddItem();

    [RelayCommand]
    private void OpenItem(Item item) => _nav.PushDetail(item.Id);
}
