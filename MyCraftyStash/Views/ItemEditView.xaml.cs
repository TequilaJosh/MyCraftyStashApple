using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ItemEditView : ContentView
{
    public ItemEditView(ItemEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
