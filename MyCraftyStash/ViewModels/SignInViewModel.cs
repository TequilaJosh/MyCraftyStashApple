using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

public partial class SignInViewModel : ObservableObject
{
    private readonly StashSession _session;
    private readonly StashApi _api;

    public SignInViewModel(StashSession session, StashApi api)
    {
        _session = session;
        _api = api;
        ApiKey = string.Empty;
    }

    [ObservableProperty] public partial string ApiKey { get; set; }
    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    [RelayCommand]
    private async Task Connect()
    {
        if (string.IsNullOrWhiteSpace(ApiKey)) return;
        Busy = true;
        ErrorMessage = null;
        try
        {
            await _session.SignInAsync(_api, ApiKey);
            await Shell.Current.GoToAsync("//inventory");
        }
        catch (StashApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception)
        {
            ErrorMessage = "Couldn't reach the stash service. Check your internet connection and try again.";
        }
        finally
        {
            Busy = false;
        }
    }
}
