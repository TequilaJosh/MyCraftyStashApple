using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class EnvelopeExpertView : ContentView
{
    public EnvelopeExpertView(EnvelopeExpertViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
