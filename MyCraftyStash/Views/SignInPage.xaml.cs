using MyCraftyStash.ViewModels;

namespace MyCraftyStash.Views;

public partial class SignInPage : ContentPage
{
    public SignInPage(SignInViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
