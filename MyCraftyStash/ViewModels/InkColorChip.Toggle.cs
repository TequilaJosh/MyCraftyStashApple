using CommunityToolkit.Mvvm.Input;

namespace MyCraftyStash.ViewModels;

// MAUI-side helper: chips toggle by tap (the desktop used ToggleButton).
public partial class InkColorChip
{
    [RelayCommand]
    private void Toggle() => IsSelected = !IsSelected;
}
