using MyCraftyStash.Services;

namespace MyCraftyStash;

public partial class App : Application
{
    private readonly StashSession _session;

    public App(StashSession session)
    {
        InitializeComponent();
        _session = session;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        var window = new Window(shell);

        // The Shell starts on the sign-in page; skip it when a key is already
        // stored. SecureStorage is async, so this runs once the window exists.
        window.Created += async (_, _) =>
        {
            await _session.InitializeAsync();
            if (_session.IsSignedIn)
                await shell.GoToAsync("//inventory");
        };

        return window;
    }
}
