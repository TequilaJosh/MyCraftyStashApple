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

    // Card build ("How it was made")
    public ObservableCollection<ProjectCardBuildStep> CardSteps { get; } = new();
    [ObservableProperty] public partial bool HasCardBuild { get; set; }
    [ObservableProperty] public partial string BuildCardButtonText { get; set; } = "Build the card";

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

        // Card build summary
        CardSteps.Clear();
        var build = await _cardService.GetForProjectAsync(_id);
        if (build is not null)
            foreach (var s in build.Steps)
                CardSteps.Add(s);
        HasCardBuild = CardSteps.Count > 0;
        BuildCardButtonText = HasCardBuild ? "Edit the build" : "Build the card";
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
