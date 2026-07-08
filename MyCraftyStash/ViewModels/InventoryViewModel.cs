using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Read-only inventory browser: card grid + search, paged from /api/items.</summary>
public partial class InventoryViewModel : ObservableObject
{
    private readonly StashApi _api;
    private int _page = 1;
    private int _totalPages = 1;

    public InventoryViewModel(StashApi api)
    {
        _api = api;
        SearchText = string.Empty;
    }

    public ObservableCollection<StashItem> Items { get; } = new();

    [ObservableProperty] public partial string SearchText { get; set; }
    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }

    private bool _loadedOnce;

    /// <summary>First load when the page appears; no-op afterwards.</summary>
    [RelayCommand]
    private async Task Appearing()
    {
        if (_loadedOnce) return;
        _loadedOnce = true;
        await Refresh();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await Load(page: 1);
        IsRefreshing = false;
    }

    [RelayCommand]
    private async Task LoadMore()
    {
        if (Busy || _page >= _totalPages) return;
        await Load(page: _page + 1);
    }

    [RelayCommand]
    private async Task OpenItem(StashItem item)
    {
        await Shell.Current.GoToAsync("itemdetail", new Dictionary<string, object> { ["Item"] = item });
    }

    private async Task Load(int page)
    {
        Busy = true;
        ErrorMessage = null;
        try
        {
            var result = await _api.GetItemsAsync(page: page, search: SearchText);
            if (page == 1) Items.Clear();
            foreach (var item in result.Items)
                Items.Add(item);
            _page = result.Page;
            _totalPages = result.TotalPages;
        }
        catch (StashApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Couldn't reach the stash service. Pull to refresh to try again.";
        }
        finally
        {
            Busy = false;
            IsEmpty = Items.Count == 0;
        }
    }
}
