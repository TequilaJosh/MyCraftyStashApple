using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class SocialView : ContentView
{
    private readonly SocialViewModel _vm;

    public SocialView(SocialViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Loaded += async (_, _) => await _vm.Refresh();
    }
}
