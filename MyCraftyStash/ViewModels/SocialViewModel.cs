using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Social container — Calendar / Address Book tabs (desktop parity).</summary>
public partial class SocialViewModel : ObservableObject, IRefreshOnReturn
{
    public CalendarViewModel CalendarVM { get; }
    public AddressBookViewModel AddressBookVM { get; }

    public SocialViewModel(CalendarViewModel calendarVm, AddressBookViewModel addressBookVm)
    {
        CalendarVM = calendarVm;
        AddressBookVM = addressBookVm;
    }

    [ObservableProperty] public partial string ActiveTab { get; set; } = "Calendar";
    public bool IsCalendarTab => ActiveTab == "Calendar";
    public bool IsAddressBookTab => ActiveTab == "AddressBook";
    partial void OnActiveTabChanged(string value)
    {
        OnPropertyChanged(nameof(IsCalendarTab));
        OnPropertyChanged(nameof(IsAddressBookTab));
    }

    [RelayCommand] private void ShowCalendar() => ActiveTab = "Calendar";
    [RelayCommand] private void ShowAddressBook() => ActiveTab = "AddressBook";

    public async Task Refresh()
    {
        await Task.WhenAll(CalendarVM.LoadCalendarCommand.ExecuteAsync(null),
                           AddressBookVM.LoadCommand.ExecuteAsync(null));
    }
}
