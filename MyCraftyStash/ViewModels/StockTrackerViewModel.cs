using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Stock Tracker: items that have a stock count, lowest first, so
/// the ones running low float to the top. Tapping opens the item's detail.</summary>
public partial class StockTrackerViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InventoryService _service;
    private readonly AppNavigator _nav;

    public StockTrackerViewModel(InventoryService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
    }

    public ObservableCollection<Item> Items { get; } = new();

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
            var items = await _service.GetStockItemsAsync();
            Items.Clear();
            foreach (var i in items)
                Items.Add(i);
        }
        finally { Busy = false; IsRefreshing = false; IsEmpty = Items.Count == 0; }
    }

    [RelayCommand]
    private Task DoRefresh() => Load();

    [RelayCommand]
    private void OpenItem(Item item) => _nav.PushDetail(item.Id);
}
