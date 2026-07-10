using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Read view for a project, with its linked "items used". Edit/Delete.</summary>
public partial class ProjectDetailViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly ProjectService _service;
    private readonly CardBuildService _cardService;
    private readonly AppNavigator _nav;
    private int _id;

    public ProjectDetailViewModel(ProjectService service, CardBuildService cardService, AppNavigator nav)
    {
        _service = service;
        _cardService = cardService;
        _nav = nav;
    }

    [ObservableProperty] public partial Project? Project { get; set; }
    public ObservableCollection<Item> ItemsUsed { get; } = new();
    [ObservableProperty] public partial bool HasItemsUsed { get; set; }
    [ObservableProperty] public partial string? DateCreatedText { get; set; }

    // Multi-image gallery (cover at index 0, then extra project_images)
    private readonly List<string> _galleryImages = new();
    public ObservableCollection<string> Thumbnails { get; } = new();
    [ObservableProperty] public partial string? CurrentImage { get; set; }
    [ObservableProperty] public partial bool HasMultipleImages { get; set; }
    private int _imageIndex;

    // Card build ("How it was made")
    public ObservableCollection<ProjectCardBuildStep> CardSteps { get; } = new();
    [ObservableProperty] public partial bool HasCardBuild { get; set; }
    [ObservableProperty] public partial string BuildCardButtonText { get; set; } = "Build the card";

    // Creations ("I Made One!")
    public ObservableCollection<ProjectCreation> Creations { get; } = new();
    [ObservableProperty] public partial bool HasCreations { get; set; }
    [ObservableProperty] public partial bool IsAddingCreation { get; set; }
    [ObservableProperty] public partial string? NewCreationNotes { get; set; }
    [ObservableProperty] public partial bool SubtractMaterials { get; set; } = true;

    public async void Init(int id)
    {
        _id = id;
        await Refresh();
    }

    public async Task Refresh()
    {
        if (_id <= 0) return;
        Project = await _service.GetAsync(_id);
        ItemsUsed.Clear();
        if (Project is not null)
        {
            foreach (var pi in Project.ProjectItems.OrderBy(x => x.SortOrder))
                if (pi.Item is not null)
                    ItemsUsed.Add(pi.Item);
        }
        HasItemsUsed = ItemsUsed.Count > 0;
        DateCreatedText = Project?.CreatedAt.ToString("MMMM d, yyyy");

        // Image gallery: cover (if any) then extra images
        _galleryImages.Clear();
        if (Project is not null && !string.IsNullOrEmpty(Project.ImageUrl))
            _galleryImages.Add(Project.ImageUrl);
        foreach (var img in await _service.GetProjectImagesAsync(_id))
            _galleryImages.Add(img.ImageUrl);
        Thumbnails.Clear();
        foreach (var g in _galleryImages) Thumbnails.Add(g);
        HasMultipleImages = _galleryImages.Count > 1;
        _imageIndex = 0;
        CurrentImage = _galleryImages.FirstOrDefault();

        // Card build summary
        CardSteps.Clear();
        var build = await _cardService.GetForProjectAsync(_id);
        if (build is not null)
            foreach (var s in build.Steps)
                CardSteps.Add(s);
        HasCardBuild = CardSteps.Count > 0;
        BuildCardButtonText = HasCardBuild ? "Edit the build" : "Build the card";

        // Creations
        Creations.Clear();
        foreach (var c in await _service.GetCreationsAsync(_id))
            Creations.Add(c);
        HasCreations = Creations.Count > 0;
    }

    // ── Gallery navigation ────────────────────────────────────────────────────

    [RelayCommand]
    private void PreviousImage()
    {
        if (_galleryImages.Count <= 1) return;
        _imageIndex = (_imageIndex - 1 + _galleryImages.Count) % _galleryImages.Count;
        CurrentImage = _galleryImages[_imageIndex];
    }

    [RelayCommand]
    private void NextImage()
    {
        if (_galleryImages.Count <= 1) return;
        _imageIndex = (_imageIndex + 1) % _galleryImages.Count;
        CurrentImage = _galleryImages[_imageIndex];
    }

    [RelayCommand]
    private void SelectImage(string image)
    {
        int idx = _galleryImages.IndexOf(image);
        if (idx < 0) return;
        _imageIndex = idx;
        CurrentImage = image;
    }

    // ── Creations ("I Made One!") ─────────────────────────────────────────────

    [RelayCommand]
    private void StartAddCreation()
    {
        NewCreationNotes = null;
        SubtractMaterials = true;
        IsAddingCreation = true;
    }

    [RelayCommand]
    private void CancelAddCreation() => IsAddingCreation = false;

    [RelayCommand]
    private async Task SaveCreation()
    {
        if (Project is null) return;

        string? materials = null;
        if (SubtractMaterials)
        {
            var summary = await _service.SubtractMaterialsForProjectAsync(Project.Id);
            materials = string.IsNullOrEmpty(summary) ? null : summary;
        }

        await _service.AddCreationAsync(new ProjectCreation
        {
            ProjectId = Project.Id,
            Notes = string.IsNullOrWhiteSpace(NewCreationNotes) ? null : NewCreationNotes.Trim(),
            MaterialsUsed = materials,
        });

        IsAddingCreation = false;
        await Refresh();
    }

    [RelayCommand]
    private async Task DeleteCreation(ProjectCreation creation)
    {
        await _service.DeleteCreationAsync(creation.Id);
        Creations.Remove(creation);
        HasCreations = Creations.Count > 0;
    }

    [RelayCommand]
    private async Task BuildCard()
    {
        if (Project is null) return;

        // Open the full card build wizard as a modal popout (desktop parity).
        var existing = await _cardService.GetForProjectAsync(Project.Id);
        var vm = new CardWizardViewModel();
        await vm.InitializeAsync(Project.Id, Project.Name, Project.ImageUrl, existing?.StateSnapshot);

        int projectId = Project.Id;
        var page = new Views.CardWizardPage(vm, async wiz =>
        {
            if (!wiz.WasConfirmed || wiz.BuildSteps.Count == 0) { await Refresh(); return; }
            await _cardService.SaveWizardBuildAsync(projectId, wiz.CardBaseType, wiz.BuildSteps, wiz.CaptureSnapshotJson());
            await Refresh();
        });
        var nav = Application.Current?.Windows[0].Page?.Navigation;
        if (nav is not null)
            await nav.PushModalAsync(page);
    }

    [RelayCommand]
    private void Edit()
    {
        if (Project is not null)
            _nav.PushProjectEdit(Project.Id);
    }

    [RelayCommand]
    private async Task Delete()
    {
        if (Project is null) return;
        var page = Application.Current?.Windows[0].Page;
        bool ok = page is not null && await page.DisplayAlert("Delete project",
            $"Delete \"{Project.Name}\"? This can't be undone.", "Delete", "Cancel");
        if (!ok) return;

        await _service.DeleteAsync(Project.Id);
        _nav.Back();
    }

    [RelayCommand]
    private void OpenItem(Item item) => _nav.PushDetail(item.Id);

    [RelayCommand]
    private void AddItemLink()
    {
        if (Project is not null)
            _nav.PushItemPicker(Project.Id);
    }

    [RelayCommand]
    private async Task RemoveItem(Item item)
    {
        if (Project is null) return;
        await _service.RemoveItemFromProjectAsync(Project.Id, item.Id);
        await Refresh();
    }

    [RelayCommand]
    private void Back() => _nav.Back();
}
