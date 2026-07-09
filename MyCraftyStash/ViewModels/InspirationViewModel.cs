using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Inspiration gallery: pick photos to save, tap to view.</summary>
public partial class InspirationViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InspirationService _service;
    private readonly AppNavigator _nav;

    public InspirationViewModel(InspirationService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
    }

    public ObservableCollection<InspirationImage> Images { get; } = new();

    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }
    [ObservableProperty] public partial string? Error { get; set; }

    public Task Refresh() => Load();

    [RelayCommand]
    public async Task Load()
    {
        Busy = true;
        try
        {
            var images = await _service.GetAllAsync();
            Images.Clear();
            foreach (var i in images)
                Images.Add(i);
        }
        finally { Busy = false; IsEmpty = Images.Count == 0; }
    }

    [RelayCommand]
    private async Task AddImage()
    {
        Error = null;
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo is null) return;

            using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var b64 = Convert.ToBase64String(ms.ToArray());

            await _service.AddAsync(new InspirationImage
            {
                ImageUrl = "data:image/jpeg;base64," + b64,
                Title = Path.GetFileNameWithoutExtension(photo.FileName),
            });
            await Load();
        }
        catch (FeatureNotSupportedException)
        {
            Error = "Photo picking isn't available on this device.";
        }
        catch (PermissionException)
        {
            Error = "Photo access was denied. You can allow it in system settings.";
        }
        catch (Exception ex)
        {
            Error = "Couldn't add that image: " + ex.Message;
        }
    }

    [RelayCommand]
    private void OpenImage(InspirationImage image) => _nav.PushInspirationDetail(image.Id);
}
