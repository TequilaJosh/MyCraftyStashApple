using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ItemDetailPage : ContentPage
{
    public ItemDetailPage(ItemDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
