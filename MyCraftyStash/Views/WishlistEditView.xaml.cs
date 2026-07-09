using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class WishlistEditView : ContentView
{
    public WishlistEditView(WishlistEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
