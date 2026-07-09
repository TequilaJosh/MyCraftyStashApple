using MyCraftyStash.ViewModels;
using MyCraftyStash.Views;

namespace MyCraftyStash;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // The custom sidebar (Shell.FlyoutContent) binds to a ShellViewModel
        // for selection highlighting + navigation.
        if (FlyoutContent is BindableObject sidebar)
            sidebar.BindingContext = new ShellViewModel();

        // Detail + edit pages are pushed on top of the inventory list.
        Routing.RegisterRoute("itemdetail", typeof(ItemDetailPage));
        Routing.RegisterRoute("itemedit", typeof(ItemEditPage));
    }
}
