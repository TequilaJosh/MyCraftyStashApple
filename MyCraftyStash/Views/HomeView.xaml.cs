using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class HomeView : ContentView
{
    private readonly HomeViewModel _vm;

    public HomeView(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        // Defer the count load off the initial layout pass. Home is the first
        // view shown at startup; loading synchronously in Loaded updates bound
        // properties mid-arrange and can fail-fast WinUI. Dispatch runs it after
        // the current layout completes.
        Loaded += (_, _) => Dispatcher.Dispatch(() => _vm.LoadCommand.Execute(null));
    }
}
