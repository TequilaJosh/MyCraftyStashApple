using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ItemDetailPage : ContentPage
{
    private readonly ItemDetailViewModel _vm;

    public ItemDetailPage(ItemDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCommand.Execute(null);
    }
}
