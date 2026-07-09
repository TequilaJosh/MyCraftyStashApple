using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class CardWizardPage : ContentPage
{
    private readonly CardWizardViewModel _vm;
    private readonly Action<CardWizardViewModel> _onClosed;

    public CardWizardPage(CardWizardViewModel vm, Action<CardWizardViewModel> onClosed)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _onClosed = onClosed;

        // The remaining sections live in a second ContentView (kept separate to
        // keep each XAML file manageable). It inherits this page's BindingContext.
        SectionsPart2.Content = new CardWizardSections2();

        _vm.CloseRequested += async (_, _) =>
        {
            await Navigation.PopModalAsync();
            _onClosed(_vm);
        };
    }
}
