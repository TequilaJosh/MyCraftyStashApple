using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ProjectEditView : ContentView
{
    public ProjectEditView(ProjectEditViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
