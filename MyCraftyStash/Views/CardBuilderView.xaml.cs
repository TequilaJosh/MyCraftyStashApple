using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class CardBuilderView : ContentView
{
    public CardBuilderView(CardBuilderViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
