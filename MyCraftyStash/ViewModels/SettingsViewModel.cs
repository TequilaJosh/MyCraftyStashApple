using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly StashSession _session;

    public SettingsViewModel(StashSession session)
    {
        _session = session;
    }

    public string SignedInAs => _session.FirstName ?? "Connected";

    [RelayCommand]
    private async Task SignOut()
    {
        _session.SignOut();
        await Shell.Current.GoToAsync("//signin");
    }
}
