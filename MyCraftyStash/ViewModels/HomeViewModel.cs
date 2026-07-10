using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>A low/out supply row on the Home dashboard.</summary>
public partial class HomeStatItem : ObservableObject
{
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public string? Number { get; set; }
    public bool HasNumber => !string.IsNullOrEmpty(Number);
    public bool IsOut { get; set; }
    public string StockLabel => IsOut ? "Out" : "Low";
    public double FillFraction { get; set; }  // 0..1 for the little bar
}

/// <summary>A recent-project card on the Home dashboard.</summary>
public partial class HomeRecentProject : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string DateText { get; set; } = "";
    public string SupplyText { get; set; } = "";
}

/// <summary>
/// Home dashboard — clone of the desktop "Welcome back" screen: four stat
/// cards (total items, sentiments indexed, low/out, projects), a Running-low
/// list and a Recent-projects grid.
/// </summary>
public partial class HomeViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InventoryService _inventory;
    private readonly ProjectService _projects;
    private readonly AppNavigator _nav;

    public HomeViewModel(InventoryService inventory, ProjectService projects, AppNavigator nav)
    {
        _inventory = inventory;
        _projects = projects;
        _nav = nav;
    }

    [ObservableProperty] public partial int TotalItems { get; set; }
    [ObservableProperty] public partial int CategoryCount { get; set; }
    [ObservableProperty] public partial int SentimentsIndexed { get; set; }
    [ObservableProperty] public partial int LowOrOutCount { get; set; }
    [ObservableProperty] public partial int ProjectCount { get; set; }
    [ObservableProperty] public partial string ItemsThisMonthText { get; set; } = "";
    [ObservableProperty] public partial string ProjectsThisMonthText { get; set; } = "";
    [ObservableProperty] public partial string CategoriesText { get; set; } = "";

    public ObservableCollection<HomeStatItem> RunningLow { get; } = new();
    public ObservableCollection<HomeRecentProject> RecentProjects { get; } = new();
    [ObservableProperty] public partial bool HasRunningLow { get; set; }
    [ObservableProperty] public partial bool HasRecentProjects { get; set; }

    public Task Refresh() => Load();

    [RelayCommand]
    public async Task Load()
    {
        var now = DateTime.Now;
        var items = await _inventory.GetItemsAsync();

        TotalItems = items.Count;
        CategoryCount = items.Select(i => i.Type ?? "").Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().Count();
        CategoriesText = $"across {CategoryCount} categories";
        SentimentsIndexed = items.Count(i => !string.IsNullOrWhiteSpace(i.Sentiments));
        int itemsThisMonth = items.Count(i => i.CreatedAt.Year == now.Year && i.CreatedAt.Month == now.Month);
        ItemsThisMonthText = $"▲ {itemsThisMonth} this month";

        // Low / out: tracked-type items with a stock count, out (≤0) or low (≤5).
        var lowList = items
            .Where(i => InventoryService.IsTrackedType(i.Type) && i.CurrentStock is int)
            .Select(i => new { Item = i, Stock = i.CurrentStock!.Value })
            .Where(x => x.Stock <= 5)
            .Select(x => new HomeStatItem
            {
                ItemId = x.Item.Id,
                Name = x.Item.Name,
                Number = x.Item.ItemNumber,
                IsOut = x.Stock <= 0,
                FillFraction = Math.Clamp(x.Stock / 10.0, 0, 1),
            })
            .OrderBy(x => x.IsOut ? 0 : 1)
            .ThenBy(x => x.Name)
            .ToList();
        LowOrOutCount = lowList.Count;
        RunningLow.Clear();
        foreach (var x in lowList.Take(5)) RunningLow.Add(x);
        HasRunningLow = RunningLow.Count > 0;

        // Projects
        var projects = await _projects.GetAllAsync();
        ProjectCount = projects.Count;
        int projThisMonth = projects.Count(p => p.CreatedAt.Year == now.Year && p.CreatedAt.Month == now.Month);
        ProjectsThisMonthText = $"▲ {projThisMonth} this month";

        RecentProjects.Clear();
        foreach (var p in projects.OrderByDescending(p => p.CreatedAt).Take(4))
        {
            int supplies = p.ProjectItems?.Count ?? 0;
            RecentProjects.Add(new HomeRecentProject
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.ImageUrl,
                DateText = p.CreatedAt.ToString("MMM d, yyyy"),
                SupplyText = $"{supplies} supplies",
            });
        }
        HasRecentProjects = RecentProjects.Count > 0;
    }

    [RelayCommand]
    private void Go(string route) => _nav.ShowSection(route);

    [RelayCommand]
    private void OpenProject(HomeRecentProject project) => _nav.PushProjectDetail(project.Id);

    [RelayCommand]
    private void OpenItem(HomeStatItem item) => _nav.PushDetail(item.ItemId);
}
