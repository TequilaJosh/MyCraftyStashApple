using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class InventoryPage : ContentPage
{
    private readonly InventoryViewModel _vm;

    public InventoryPage(InventoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.AppearingCommand.Execute(null);
    }
}
