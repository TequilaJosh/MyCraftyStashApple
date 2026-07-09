using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class InspirationDetailView : ContentView
{
    public InspirationDetailView(InspirationDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
