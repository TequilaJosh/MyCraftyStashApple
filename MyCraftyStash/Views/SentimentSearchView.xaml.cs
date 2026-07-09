using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class SentimentSearchView : ContentView
{
    public SentimentSearchView(SentimentSearchViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
