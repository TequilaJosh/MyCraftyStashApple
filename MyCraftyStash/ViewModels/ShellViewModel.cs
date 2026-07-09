using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyCraftyStash.ViewModels;

/// <summary>
/// Backs the custom sidebar (Shell FlyoutContent): tracks the selected section
/// for the highlight and routes navigation. Real sections resolve to their
/// ShellContent route; not-yet-ported sections land on the Coming Soon pane.
/// </summary>
public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty] public partial string CurrentRoute { get; set; } = "home";

    [RelayCommand]
    private async Task Navigate(string route)
    {
        CurrentRoute = route;
        await Shell.Current.GoToAsync($"//{route}");
        Shell.Current.FlyoutIsPresented = false; // no-op when locked; tidy on mobile
    }

    [RelayCommand]
    private async Task AddItem()
    {
        CurrentRoute = "inventory";
        await Shell.Current.GoToAsync("//inventory");
        await Shell.Current.GoToAsync("itemedit");
    }

    [RelayCommand]
    private Task OpenKoFi() => Launcher.Default.OpenAsync("https://ko-fi.com/mycraftystash");
}
