using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class WishlistView : ContentView
{
    private readonly WishlistViewModel _vm;

    public WishlistView(WishlistViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Loaded += (_, _) => _vm.LoadCommand.Execute(null);
    }
}
