using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>The inventory grid: local search + card list, with add/open.</summary>
public partial class InventoryViewModel : ObservableObject
{
    private readonly InventoryService _service;

    public InventoryViewModel(InventoryService service)
    {
        _service = service;
        SearchText = string.Empty;
    }

    public ObservableCollection<Item> Items { get; } = new();

    [ObservableProperty] public partial string SearchText { get; set; }
    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }

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
    private Task Refresh() => Load();

    [RelayCommand]
    private Task Search() => Load();

    [RelayCommand]
    private Task AddItem() => Shell.Current.GoToAsync("itemedit");

    [RelayCommand]
    private Task OpenItem(Item item) =>
        Shell.Current.GoToAsync("itemdetail", new Dictionary<string, object> { ["Id"] = item.Id });
}
