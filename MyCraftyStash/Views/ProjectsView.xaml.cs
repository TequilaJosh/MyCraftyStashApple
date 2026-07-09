using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ProjectsView : ContentView
{
    private readonly ProjectsViewModel _vm;

    public ProjectsView(ProjectsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        Loaded += (_, _) => _vm.LoadCommand.Execute(null);
    }
}
