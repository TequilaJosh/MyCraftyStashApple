using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Backs the persistent sidebar: selection highlight + navigation.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly AppNavigator _nav;

    public MainViewModel(AppNavigator nav)
    {
        _nav = nav;
        _nav.Changed += () => CurrentRoute = _nav.CurrentRoute;
    }

    [ObservableProperty] public partial string CurrentRoute { get; set; } = "home";

    [RelayCommand]
    private void Navigate(string route) => _nav.ShowSection(route);

    [RelayCommand]
    private void AddItem()
    {
        _nav.ShowSection("inventory");
        _nav.PushAddItem();
    }

    [RelayCommand]
    private Task OpenKoFi() => Launcher.Default.OpenAsync("https://ko-fi.com/mycraftystash");
}
