using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Find items by the sentiment text stored on them.</summary>
public partial class SentimentSearchViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InventoryService _service;
    private readonly AppNavigator _nav;

    public SentimentSearchViewModel(InventoryService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
        SearchText = string.Empty;
    }

    public ObservableCollection<Item> Results { get; } = new();

    [ObservableProperty] public partial string SearchText { get; set; }
    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial bool HasSearched { get; set; }

    // Re-run the last search when returning from an item's detail.
    public Task Refresh() => HasSearched ? Search() : Task.CompletedTask;

    [RelayCommand]
    private async Task Search()
    {
        Busy = true;
        try
        {
            var items = await _service.SearchBySentimentAsync(SearchText);
            Results.Clear();
            foreach (var i in items)
                Results.Add(i);
            HasSearched = true;
        }
        finally { Busy = false; }
    }

    [RelayCommand]
    private void OpenItem(Item item) => _nav.PushDetail(item.Id);
}
