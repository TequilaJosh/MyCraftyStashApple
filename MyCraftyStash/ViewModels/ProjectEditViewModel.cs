using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Add/edit a project's basic fields. Item linking is a later port.</summary>
public partial class ProjectEditViewModel : ObservableObject
{
    private readonly ProjectService _service;
    private readonly AppNavigator _nav;
    private int _id;

    public ProjectEditViewModel(ProjectService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
        Name = "";
    }

    [ObservableProperty] public partial string PageTitle { get; set; } = "Add project";
    [ObservableProperty] public partial bool IsEditing { get; set; }
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial string? Description { get; set; }
    [ObservableProperty] public partial string? Technique { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }
    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public async void Init(int id)
    {
        if (id <= 0) return;
        _id = id;
        IsEditing = true;
        PageTitle = "Edit project";
        var p = await _service.GetAsync(id);
        if (p is not null)
        {
            Name = p.Name;
            Description = p.Description;
            Technique = p.Technique;
            Notes = p.Notes;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Name is required."; return; }

        if (_id > 0)
        {
            var p = await _service.GetAsync(_id);
            if (p is null) { _nav.Back(); return; }
            p.Name = Name.Trim();
            p.Description = Blank(Description);
            p.Technique = Blank(Technique);
            p.Notes = Blank(Notes);
            await _service.UpdateAsync(p);
        }
        else
        {
            await _service.AddAsync(new Project
            {
                Name = Name.Trim(),
                Description = Blank(Description),
                Technique = Blank(Technique),
                Notes = Blank(Notes),
            });
        }

        _nav.Back();
    }

    [RelayCommand]
    private void Cancel() => _nav.Back();

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
