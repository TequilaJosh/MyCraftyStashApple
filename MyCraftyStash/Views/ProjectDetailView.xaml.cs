using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class ProjectDetailView : ContentView
{
    public ProjectDetailView(ProjectDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
