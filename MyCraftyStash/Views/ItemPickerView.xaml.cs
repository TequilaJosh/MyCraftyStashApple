using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ItemPickerView : ContentView
{
    public ItemPickerView(ItemPickerViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
