using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class HomeView : ContentView
{
    private readonly HomeViewModel _vm;

    public HomeView(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Loaded += (_, _) => _vm.LoadCommand.Execute(null);
    }
}
