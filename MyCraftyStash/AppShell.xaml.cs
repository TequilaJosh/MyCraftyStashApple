using MyCraftyStash.Views;

namespace MyCraftyStash;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("itemdetail", typeof(ItemDetailPage));
    }
}
