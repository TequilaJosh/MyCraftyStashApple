using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Home dashboard: at-a-glance counts + quick links into each section.</summary>
public partial class HomeViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InventoryService _inventory;
    private readonly ProjectService _projects;
    private readonly WishlistService _wishlist;
    private readonly AppNavigator _nav;

    public HomeViewModel(InventoryService inventory, ProjectService projects,
        WishlistService wishlist, AppNavigator nav)
    {
        _inventory = inventory;
        _projects = projects;
        _wishlist = wishlist;
        _nav = nav;
    }

    [ObservableProperty] public partial int ItemCount { get; set; }
    [ObservableProperty] public partial int ProjectCount { get; set; }
    [ObservableProperty] public partial int WishlistCount { get; set; }
    [ObservableProperty] public partial int LowStockCount { get; set; }

    public Task Refresh() => Load();

    [RelayCommand]
    public async Task Load()
    {
        ItemCount = await _inventory.GetItemCountAsync();
        ProjectCount = await _projects.GetCountAsync();
        WishlistCount = await _wishlist.GetCountAsync();
        LowStockCount = await _inventory.GetLowStockCountAsync();
    }

    [RelayCommand]
    private void Go(string route) => _nav.ShowSection(route);
}
