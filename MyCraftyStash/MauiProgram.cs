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
        builder.Services.AddSingleton<InventoryService>();

        // ViewModels
        builder.Services.AddSingleton<InventoryViewModel>();
        builder.Services.AddTransient<ItemDetailViewModel>();
        builder.Services.AddTransient<ItemEditViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Pages
        builder.Services.AddSingleton<InventoryPage>();
        builder.Services.AddTransient<ItemDetailPage>();
        builder.Services.AddTransient<ItemEditPage>();
        builder.Services.AddTransient<SettingsPage>();

        // Create the local database on first run (no-op afterwards).
        new InventoryService().InitializeAsync().GetAwaiter().GetResult();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
