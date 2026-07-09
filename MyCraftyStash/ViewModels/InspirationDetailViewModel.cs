using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

public partial class InspirationDetailViewModel : ObservableObject
{
    private readonly InspirationService _service;
    private readonly AppNavigator _nav;
    private int _id;

    public InspirationDetailViewModel(InspirationService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
    }

    [ObservableProperty] public partial InspirationImage? Image { get; set; }
    [ObservableProperty] public partial string? Title { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }

    public async void Init(int id)
    {
        _id = id;
        Image = await _service.GetAsync(id);
        Title = Image?.Title;
        Notes = Image?.Notes;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (Image is null) return;
        Image.Title = string.IsNullOrWhiteSpace(Title) ? null : Title.Trim();
        Image.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();
        await _service.UpdateAsync(Image);
        _nav.Back();
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (_id <= 0) return;
        var page = Application.Current?.Windows[0].Page;
        bool ok = page is not null && await page.DisplayAlert("Remove image",
            "Remove this inspiration image?", "Remove", "Cancel");
        if (!ok) return;

        await _service.DeleteAsync(_id);
        _nav.Back();
    }

    [RelayCommand]
    private void Back() => _nav.Back();
}
