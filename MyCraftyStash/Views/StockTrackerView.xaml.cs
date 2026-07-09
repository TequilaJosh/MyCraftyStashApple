using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class StockTrackerView : ContentView
{
    private readonly StockTrackerViewModel _vm;

    public StockTrackerView(StockTrackerViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Loaded += (_, _) => _vm.LoadCommand.Execute(null);
    }
}
