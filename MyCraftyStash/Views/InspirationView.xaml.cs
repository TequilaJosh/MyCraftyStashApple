using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class InspirationView : ContentView
{
    private readonly InspirationViewModel _vm;

    public InspirationView(InspirationViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Loaded += (_, _) => _vm.LoadCommand.Execute(null);
    }
}
