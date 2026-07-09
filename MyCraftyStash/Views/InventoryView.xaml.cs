using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class InventoryView : ContentView
{
    private readonly InventoryViewModel _vm;

    public InventoryView(InventoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        // Reload whenever the view is shown in the content pane.
        Loaded += (_, _) => _vm.LoadCommand.Execute(null);
    }
}
