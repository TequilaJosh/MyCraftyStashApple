using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class SettingsView : ContentView
{
    private readonly SettingsViewModel _vm;

    public SettingsView(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Loaded += (_, _) => _vm.LoadCommand.Execute(null);
    }
}
