using MyCraftyStash.Views;

namespace MyCraftyStash;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        // Detail + edit pages are pushed on top of the inventory list.
        Routing.RegisterRoute("itemdetail", typeof(ItemDetailPage));
        Routing.RegisterRoute("itemedit", typeof(ItemEditPage));
    }
}
