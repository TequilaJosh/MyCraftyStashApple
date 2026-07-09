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
        builder.Services.AddSingleton<WishlistService>();
        builder.Services.AddSingleton<AppNavigator>();

        // Shell / sidebar
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

        // Section views (kept alive so their state persists)
        builder.Services.AddSingleton<HomeViewModel>();
        builder.Services.AddSingleton<HomeView>();
        builder.Services.AddSingleton<InventoryViewModel>();
        builder.Services.AddSingleton<InventoryView>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<SettingsView>();
        builder.Services.AddSingleton<WishlistViewModel>();
        builder.Services.AddSingleton<WishlistView>();
        builder.Services.AddSingleton<StockTrackerViewModel>();
        builder.Services.AddSingleton<StockTrackerView>();
        builder.Services.AddSingleton<SentimentSearchViewModel>();
        builder.Services.AddSingleton<SentimentSearchView>();
        builder.Services.AddSingleton<InspirationViewModel>();
        builder.Services.AddSingleton<InspirationView>();
        builder.Services.AddSingleton<ProjectsViewModel>();
        builder.Services.AddSingleton<ProjectsView>();
        builder.Services.AddSingleton<EnvelopeExpertViewModel>();
        builder.Services.AddSingleton<EnvelopeExpertView>();
        builder.Services.AddTransient<ReportViewModel>();
        builder.Services.AddTransient<ReportView>();
        builder.Services.AddTransient<ComingSoonView>();

        // Pushed sub-views (fresh per navigation)
        builder.Services.AddTransient<ItemDetailViewModel>();
        builder.Services.AddTransient<ItemDetailView>();
        builder.Services.AddTransient<ItemEditViewModel>();
        builder.Services.AddTransient<ItemEditView>();
        builder.Services.AddTransient<WishlistEditViewModel>();
        builder.Services.AddTransient<WishlistEditView>();
        builder.Services.AddTransient<InspirationDetailViewModel>();
        builder.Services.AddTransient<InspirationDetailView>();
        builder.Services.AddTransient<ProjectDetailViewModel>();
        builder.Services.AddTransient<ProjectDetailView>();
        builder.Services.AddTransient<ProjectEditViewModel>();
        builder.Services.AddTransient<ProjectEditView>();

        // Create the local database on first run (no-op afterwards).
        new InventoryService().InitializeAsync().GetAwaiter().GetResult();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
