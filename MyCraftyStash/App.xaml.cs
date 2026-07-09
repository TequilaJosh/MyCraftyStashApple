using Microsoft.Extensions.DependencyInjection;

namespace MyCraftyStash;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
        // The desktop app's signature look is the warm light theme; pin to it
        // so the custom sidebar/palette renders consistently on every device.
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_services.GetRequiredService<MainPage>());
    }
}
