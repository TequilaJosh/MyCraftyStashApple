using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ColorMatchView : ContentView
{
    private readonly ColorMatchViewModel _vm;

    public ColorMatchView(ColorMatchViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Loaded += async (_, _) => await _vm.Refresh();
    }
}
