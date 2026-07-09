using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ItemDetailView : ContentView
{
    public ItemDetailView(ItemDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
