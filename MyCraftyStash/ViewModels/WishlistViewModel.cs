using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>The wish-list grid: local search + card list, with add/open.</summary>
public partial class WishlistViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly WishlistService _service;
    private readonly AppNavigator _nav;

    public WishlistViewModel(WishlistService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
        SearchText = string.Empty;
    }

    public ObservableCollection<WishlistItem> Items { get; } = new();

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
            var items = await _service.GetAllAsync(SearchText);
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
    private void AddItem() => _nav.PushWishlistEdit(0);

    [RelayCommand]
    private void OpenItem(WishlistItem item) => _nav.PushWishlistEdit(item.Id);
}
