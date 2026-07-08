using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ItemEditPage : ContentPage
{
    public ItemEditPage(ItemEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
