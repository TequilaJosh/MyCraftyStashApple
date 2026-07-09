using MyCraftyStash.Services;
using MyCraftyStash.ViewModels;

namespace MyCraftyStash;

public partial class MainPage : ContentPage
{
    private readonly AppNavigator _nav;

    public MainPage(MainViewModel vm, AppNavigator nav)
    {
        InitializeComponent();
        BindingContext = vm;
        _nav = nav;
        _nav.Changed += () => Host.Content = _nav.Current;
        _nav.ShowSection("home"); // open on Home, like the desktop
    }
}
