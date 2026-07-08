using Microsoft.Extensions.Logging;
using MyCraftyStash.Services;
using MyCraftyStash.ViewModels;
using MyCraftyStash.Views;

namespace MyCraftyStash;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<StashSession>();
        builder.Services.AddSingleton<StashApi>();

        // ViewModels
        builder.Services.AddTransient<SignInViewModel>();
        builder.Services.AddSingleton<InventoryViewModel>();
        builder.Services.AddTransient<ItemDetailViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Pages (resolved by Shell via DI)
        builder.Services.AddTransient<SignInPage>();
        builder.Services.AddSingleton<InventoryPage>();
        builder.Services.AddTransient<ItemDetailPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
