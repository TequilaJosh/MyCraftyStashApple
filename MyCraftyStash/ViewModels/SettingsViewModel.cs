using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly InventoryService _service;

    public SettingsViewModel(InventoryService service)
    {
        _service = service;
    }

    [ObservableProperty] public partial int ItemCount { get; set; }
    public string DatabaseLocation => AppPaths.InventoryDbPath;

    [RelayCommand]
    public async Task Load()
    {
        ItemCount = await _service.GetItemCountAsync();
    }
}
