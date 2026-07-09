using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>The projects gallery: searchable cards, add/open.</summary>
public partial class ProjectsViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly ProjectService _service;
    private readonly AppNavigator _nav;

    public ProjectsViewModel(ProjectService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
        SearchText = string.Empty;
    }

    public ObservableCollection<Project> Projects { get; } = new();

    [ObservableProperty] public partial string SearchText { get; set; }
    [ObservableProperty] public partial bool Busy { get; set; }
    [ObservableProperty] public partial bool IsRefreshing { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }

    public Task Refresh() => Load();

    [RelayCommand]
    public async Task Load()
    {
        Busy = true;
        try
        {
            var projects = await _service.GetAllAsync(SearchText);
            Projects.Clear();
            foreach (var p in projects)
                Projects.Add(p);
        }
        finally { Busy = false; IsRefreshing = false; IsEmpty = Projects.Count == 0; }
    }

    [RelayCommand]
    private Task DoRefresh() => Load();

    [RelayCommand]
    private Task Search() => Load();

    [RelayCommand]
    private void AddProject() => _nav.PushProjectEdit(0);

    [RelayCommand]
    private void OpenProject(Project project) => _nav.PushProjectDetail(project.Id);
}
