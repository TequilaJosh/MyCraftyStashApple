using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>
/// Address book — clone of the desktop contact manager: searchable list on the
/// left, a read detail / inline add-edit form on the right. First name required;
/// everything else optional.
/// </summary>
public partial class AddressBookViewModel : ObservableObject
{
    private readonly AddressBookService _service;

    public AddressBookViewModel(AddressBookService service) => _service = service;

    public ObservableCollection<AddressBookEntry> Entries { get; } = new();
    [ObservableProperty] public partial AddressBookEntry? SelectedEntry { get; set; }
    [ObservableProperty] public partial bool HasSelection { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? SearchText { get; set; }
    [ObservableProperty] public partial string? StatusMessage { get; set; }

    partial void OnSearchTextChanged(string? value) => _ = Load();

    // Form
    [ObservableProperty] public partial bool IsFormOpen { get; set; }
    [ObservableProperty] public partial string FormHeading { get; set; } = "Add New Person";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string? EditFirstName { get; set; }
    [ObservableProperty] public partial string? EditLastName { get; set; }
    [ObservableProperty] public partial string? EditAddressLine1 { get; set; }
    [ObservableProperty] public partial string? EditAddressLine2 { get; set; }
    [ObservableProperty] public partial string? EditCity { get; set; }
    [ObservableProperty] public partial string? EditState { get; set; }
    [ObservableProperty] public partial string? EditZipCode { get; set; }
    [ObservableProperty] public partial string? EditCountry { get; set; }
    [ObservableProperty] public partial string? EditPhone { get; set; }
    [ObservableProperty] public partial string? EditEmail { get; set; }
    [ObservableProperty] public partial string? EditNotes { get; set; }

    [RelayCommand]
    public async Task Load()
    {
        IsLoading = true;
        try
        {
            var entries = await _service.GetAllAsync(SearchText);
            Entries.Clear();
            foreach (var e in entries) Entries.Add(e);
            IsEmpty = Entries.Count == 0;
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = null;

    [RelayCommand]
    private void SelectEntry(AddressBookEntry entry)
    {
        foreach (var e in Entries) e.IsSelected = false;
        entry.IsSelected = true;
        SelectedEntry = entry;
        HasSelection = true;
        IsFormOpen = false;
    }

    [RelayCommand]
    private void StartAdd()
    {
        FormHeading = "Add New Person";
        ErrorMessage = null;
        EditFirstName = EditLastName = EditAddressLine1 = EditAddressLine2 = null;
        EditCity = EditState = EditZipCode = EditCountry = EditPhone = EditEmail = EditNotes = null;
        _editingId = 0;
        IsFormOpen = true;
    }

    private int _editingId;

    [RelayCommand]
    private void StartEdit()
    {
        if (SelectedEntry is null) return;
        var e = SelectedEntry;
        _editingId = e.Id;
        FormHeading = "Edit Person";
        ErrorMessage = null;
        EditFirstName = e.FirstName; EditLastName = e.LastName;
        EditAddressLine1 = e.AddressLine1; EditAddressLine2 = e.AddressLine2;
        EditCity = e.City; EditState = e.State; EditZipCode = e.ZipCode; EditCountry = e.Country;
        EditPhone = e.Phone; EditEmail = e.Email; EditNotes = e.Notes;
        IsFormOpen = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsFormOpen = false;

    [RelayCommand]
    private async Task SaveEntry()
    {
        if (string.IsNullOrWhiteSpace(EditFirstName))
        {
            ErrorMessage = "First name is required.";
            return;
        }

        var entry = _editingId > 0 ? await _service.GetAsync(_editingId) ?? new AddressBookEntry() : new AddressBookEntry();
        entry.FirstName = EditFirstName.Trim();
        entry.LastName = Blank(EditLastName);
        entry.AddressLine1 = Blank(EditAddressLine1);
        entry.AddressLine2 = Blank(EditAddressLine2);
        entry.City = Blank(EditCity);
        entry.State = Blank(EditState);
        entry.ZipCode = Blank(EditZipCode);
        entry.Country = Blank(EditCountry);
        entry.Phone = Blank(EditPhone);
        entry.Email = Blank(EditEmail);
        entry.Notes = Blank(EditNotes);

        int id;
        if (_editingId > 0) { await _service.UpdateAsync(entry); id = _editingId; }
        else id = await _service.AddAsync(entry);

        StatusMessage = $"Saved {entry.FirstName}.";
        IsFormOpen = false;
        await Load();
        var saved = Entries.FirstOrDefault(e => e.Id == id);
        if (saved is not null) SelectEntry(saved);
    }

    [RelayCommand]
    private async Task DeleteEntry()
    {
        if (SelectedEntry is null) return;
        var name = SelectedEntry.FirstName;
        await _service.DeleteAsync(SelectedEntry.Id);
        SelectedEntry = null;
        HasSelection = false;
        StatusMessage = $"Deleted {name}.";
        await Load();
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
