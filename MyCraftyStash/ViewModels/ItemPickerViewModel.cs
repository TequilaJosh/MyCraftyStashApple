using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Pick an inventory item to link to a project.</summary>
public partial class ItemPickerViewModel : ObservableObject
{
    private readonly ProjectService _service;
    private readonly AppNavigator _nav;
    private int _projectId;

    public ItemPickerViewModel(ProjectService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
        SearchText = string.Empty;
    }

    public ObservableCollection<Item> Items { get; } = new();

    [ObservableProperty] public partial string SearchText { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }

    public async void Init(int projectId)
    {
        _projectId = projectId;
        await Load();
    }

    [RelayCommand]
    private async Task Load()
    {
        var items = await _service.GetLinkableItemsAsync(_projectId, SearchText);
        Items.Clear();
        foreach (var i in items)
            Items.Add(i);
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private Task Search() => Load();

    [RelayCommand]
    private async Task Pick(Item item)
    {
        await _service.AddItemToProjectAsync(_projectId, item.Id);
        _nav.Back(); // back to the project detail, which reloads its items
    }

    [RelayCommand]
    private void Cancel() => _nav.Back();
}
