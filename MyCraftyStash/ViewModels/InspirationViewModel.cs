using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>
/// Inspiration board — clone of the desktop's board-organized gallery:
/// nestable boards with a breadcrumb, a metadata-rich add form (board/color/
/// type/theme multi-selects + sentiment + items-used), item linking, a
/// metadata detail popup, search/type/theme filters, and an organize/bulk-move
/// mode. In-page overlays replace the desktop's ZIndex modals.
/// </summary>
public partial class InspirationViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InspirationService _service;
    private readonly InventoryService _inventory;
    private readonly AppNavigator _nav;

    public InspirationViewModel(InspirationService service, InventoryService inventory, AppNavigator nav)
    {
        _service = service;
        _inventory = inventory;
        _nav = nav;
    }

    // ── Board view state ────────────────────────────────────────────────────
    private int? _currentBoardId;
    private List<InspirationImage> _allImagesInBoard = new();   // pre-filter cache

    public ObservableCollection<BoardCard> Boards { get; } = new();
    public ObservableCollection<ImageCard> Images { get; } = new();
    public ObservableCollection<BreadcrumbEntry> Breadcrumb { get; } = new();

    [ObservableProperty] public partial bool HasBoards { get; set; }
    [ObservableProperty] public partial bool ShowBreadcrumb { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool IsGalleryEmpty { get; set; }

    // ── Filters ──────────────────────────────────────────────────────────────
    public ObservableCollection<string> TypeOptions { get; } = new();
    public ObservableCollection<string> ThemeOptions { get; } = new();
    [ObservableProperty] public partial string? SearchText { get; set; }
    [ObservableProperty] public partial string? FilterType { get; set; }
    [ObservableProperty] public partial string? FilterTheme { get; set; }

    partial void OnSearchTextChanged(string? value) => _ = ApplyFilters();
    partial void OnFilterTypeChanged(string? value) => _ = ApplyFilters();
    partial void OnFilterThemeChanged(string? value) => _ = ApplyFilters();

    // ═════════════════════════ LOAD / NAVIGATE ═════════════════════════

    public Task Refresh() => Load();

    [RelayCommand]
    public async Task Load()
    {
        IsLoading = true;
        try
        {
            if (TypeOptions.Count == 0)
            {
                foreach (var t in await _service.GetTypeOptionsAsync()) TypeOptions.Add(t);
                foreach (var t in await _service.GetThemeOptionsAsync()) ThemeOptions.Add(t);
            }

            // Boards at this level
            Boards.Clear();
            foreach (var b in await _service.GetBoardsAtLevelAsync(_currentBoardId))
            {
                var stats = await _service.GetBoardStatsAsync(b.Id);
                Boards.Add(new BoardCard
                {
                    Id = b.Id,
                    Name = b.Name,
                    HasDefaults = b.HasDefaults,
                    CoverImageUrl = stats.CoverImageUrl,
                    CountLabel = stats.ImageCount switch { 0 => "No images", 1 => "1 image", var n => $"{n} images" },
                    ChildLabel = stats.ChildBoardCount switch { 0 => "", 1 => "1 board", var n => $"{n} boards" },
                });
            }
            HasBoards = Boards.Count > 0;

            // Breadcrumb
            Breadcrumb.Clear();
            Breadcrumb.Add(new BreadcrumbEntry { BoardId = null, Name = "Inspiration Station", ShowSeparator = false, IsLast = _currentBoardId is null });
            if (_currentBoardId is int cur)
            {
                var path = await _service.GetBoardPathAsync(cur);
                for (int i = 0; i < path.Count; i++)
                    Breadcrumb.Add(new BreadcrumbEntry { BoardId = path[i].Id, Name = path[i].Name, ShowSeparator = true, IsLast = i == path.Count - 1 });
            }
            ShowBreadcrumb = _currentBoardId is not null;

            // Images
            _allImagesInBoard = await _service.GetImagesForBoardAsync(_currentBoardId);
            await ApplyFilters();
        }
        finally { IsLoading = false; }
    }

    private async Task ApplyFilters()
    {
        IEnumerable<InspirationImage> q = _allImagesInBoard;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var s = SearchText.Trim();
            q = q.Where(i => i.Title != null && i.Title.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(FilterType) || !string.IsNullOrWhiteSpace(FilterTheme))
        {
            var ids = await _service.GetImageIdsByItemFilterAsync(FilterType, FilterTheme);
            q = q.Where(i => ids.Contains(i.Id));
        }

        Images.Clear();
        foreach (var i in q)
            Images.Add(new ImageCard { Id = i.Id, ImageUrl = i.ImageUrl, Title = i.Title });
        IsGalleryEmpty = Images.Count == 0 && Boards.Count == 0;
    }

    [RelayCommand]
    private async Task OpenBoard(BoardCard board)
    {
        _currentBoardId = board.Id;
        SearchText = null; FilterType = null; FilterTheme = null;
        await Load();
    }

    [RelayCommand]
    private async Task NavigateBreadcrumb(BreadcrumbEntry entry)
    {
        if (entry.IsLast) return;
        _currentBoardId = entry.BoardId;
        await Load();
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        SearchText = null; FilterType = null; FilterTheme = null;
        await ApplyFilters();
    }

    // ═════════════════════════ ADD IMAGE ═════════════════════════

    [ObservableProperty] public partial bool IsAddingImage { get; set; }
    [ObservableProperty] public partial string? NewImageUrl { get; set; }
    [ObservableProperty] public partial string? NewImageSentiment { get; set; }
    public bool CanSaveImage => !string.IsNullOrEmpty(NewImageUrl);
    partial void OnNewImageUrlChanged(string? value) => OnPropertyChanged(nameof(CanSaveImage));

    public ObservableCollection<CheckOption> AddBoardOptions { get; } = new();
    public ObservableCollection<CheckOption> AddColorOptions { get; } = new();
    public ObservableCollection<CheckOption> AddTeColorOptions { get; } = new();
    public ObservableCollection<CheckOption> AddTypeOptions { get; } = new();
    public ObservableCollection<CheckOption> AddThemeOptions { get; } = new();
    public ObservableCollection<ItemTag> AddItemTags { get; } = new();

    [ObservableProperty] public partial bool BoardPickerOpen { get; set; }
    [ObservableProperty] public partial bool ColorPickerOpen { get; set; }
    [ObservableProperty] public partial bool TeColorPickerOpen { get; set; }
    [ObservableProperty] public partial bool TypePickerOpen { get; set; }
    [ObservableProperty] public partial bool ThemePickerOpen { get; set; }

    [ObservableProperty] public partial string BoardsDisplay { get; set; } = "No board (uncategorized)";
    [ObservableProperty] public partial string ColorsDisplay { get; set; } = "Select colors...";
    [ObservableProperty] public partial string TeColorsDisplay { get; set; } = "Select TE colors...";
    [ObservableProperty] public partial string TypesDisplay { get; set; } = "Select types...";
    [ObservableProperty] public partial string ThemesDisplay { get; set; } = "Select themes...";

    [RelayCommand] private void ToggleBoardPicker() => BoardPickerOpen = !BoardPickerOpen;
    [RelayCommand] private void ToggleColorPicker() => ColorPickerOpen = !ColorPickerOpen;
    [RelayCommand] private void ToggleTeColorPicker() => TeColorPickerOpen = !TeColorPickerOpen;
    [RelayCommand] private void ToggleTypePicker() => TypePickerOpen = !TypePickerOpen;
    [RelayCommand] private void ToggleThemePicker() => ThemePickerOpen = !ThemePickerOpen;

    private static string Join(IEnumerable<CheckOption> opts, string placeholder)
    {
        var sel = opts.Where(o => o.IsSelected).Select(o => o.Label).ToList();
        return sel.Count == 0 ? placeholder : string.Join(", ", sel);
    }

    [RelayCommand]
    private void ToggleBoardOption(CheckOption o) { o.IsSelected = !o.IsSelected; BoardsDisplay = Join(AddBoardOptions, "No board (uncategorized)"); }
    [RelayCommand]
    private void ToggleColorOption(CheckOption o) { o.IsSelected = !o.IsSelected; ColorsDisplay = Join(AddColorOptions, "Select colors..."); }
    [RelayCommand]
    private void ToggleTeColorOption(CheckOption o) { o.IsSelected = !o.IsSelected; TeColorsDisplay = Join(AddTeColorOptions, "Select TE colors..."); }
    [RelayCommand]
    private void ToggleTypeOption(CheckOption o) { o.IsSelected = !o.IsSelected; TypesDisplay = Join(AddTypeOptions, "Select types..."); }
    [RelayCommand]
    private void ToggleThemeOption(CheckOption o) { o.IsSelected = !o.IsSelected; ThemesDisplay = Join(AddThemeOptions, "Select themes..."); }

    [RelayCommand]
    private async Task StartAddImage()
    {
        // Reset the form
        NewImageUrl = null; NewImageSentiment = null;
        AddColorOptions.Clear(); AddTeColorOptions.Clear(); AddTypeOptions.Clear();
        AddThemeOptions.Clear(); AddBoardOptions.Clear(); AddItemTags.Clear();
        BoardPickerOpen = ColorPickerOpen = TeColorPickerOpen = TypePickerOpen = ThemePickerOpen = false;
        ColorsDisplay = "Select colors..."; TeColorsDisplay = "Select TE colors...";
        TypesDisplay = "Select types..."; ThemesDisplay = "Select themes...";
        BoardsDisplay = "No board (uncategorized)";
        CancelAddItemUsed();

        foreach (var c in InspirationService.ColorOptions) AddColorOptions.Add(new CheckOption(c));
        foreach (var c in InspirationService.ColorOptions) AddTeColorOptions.Add(new CheckOption(c));
        foreach (var t in TypeOptions) AddTypeOptions.Add(new CheckOption(t));
        foreach (var t in ThemeOptions) AddThemeOptions.Add(new CheckOption(t));

        foreach (var b in await _service.GetAllBoardsFlatAsync())
        {
            var opt = new CheckOption(b.Name, b.Id) { IsSelected = b.Id == _currentBoardId };
            AddBoardOptions.Add(opt);
        }
        BoardsDisplay = Join(AddBoardOptions, "No board (uncategorized)");

        IsAddingImage = true;
    }

    [RelayCommand]
    private void CancelAddImage() => IsAddingImage = false;

    [RelayCommand]
    private async Task PickImage()
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select an inspiration image",
                FileTypes = FilePickerFileType.Images,
            });
            if (file is null) return;
            using var stream = await file.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var ext = Path.GetExtension(file.FileName).TrimStart('.').ToLowerInvariant();
            var mime = ext switch { "jpg" or "jpeg" => "image/jpeg", "png" => "image/png",
                "gif" => "image/gif", "webp" => "image/webp", "bmp" => "image/bmp", _ => "image/jpeg" };
            NewImageUrl = $"data:{mime};base64,{Convert.ToBase64String(ms.ToArray())}";
        }
        catch (Exception) { /* cancelled / unsupported */ }
    }

    [RelayCommand]
    private async Task SaveImage()
    {
        if (string.IsNullOrEmpty(NewImageUrl)) return;

        string? colors = J(AddColorOptions), teColors = J(AddTeColorOptions),
                types = J(AddTypeOptions), themes = J(AddThemeOptions);
        string? sentiment = string.IsNullOrWhiteSpace(NewImageSentiment) ? null : NewImageSentiment.Trim();
        var boardIds = AddBoardOptions.Where(o => o.IsSelected).Select(o => (int)o.Tag!).ToList();
        var itemIds = AddItemTags.Select(t => t.Id).ToList();

        // One image per selected board (desktop parity); none selected → root.
        var targets = boardIds.Count == 0 ? new List<int?> { null } : boardIds.Select(id => (int?)id).ToList();
        foreach (var boardId in targets)
        {
            var img = new InspirationImage
            {
                ImageUrl = NewImageUrl,
                Color = colors, TeColor = teColors, Types = types, Theme = themes,
                Sentiment = sentiment, BoardId = boardId,
            };
            int id = await _service.AddAsync(img);
            if (itemIds.Count > 0) await _service.SetLinkedItemsAsync(id, itemIds);
        }

        IsAddingImage = false;
        await Load();
    }

    private static string? J(IEnumerable<CheckOption> opts)
    {
        var sel = opts.Where(o => o.IsSelected).Select(o => o.Label).ToList();
        return sel.Count == 0 ? null : string.Join(", ", sel);
    }

    // ── Items-used step wizard (add form) ──────────────────────────────────
    [ObservableProperty] public partial bool ItemStepActive { get; set; }
    [ObservableProperty] public partial bool ItemMorePrompt { get; set; }
    public ObservableCollection<string> ItemStepTypes { get; } = new();
    public ObservableCollection<Item> ItemStepOptions { get; } = new();
    [ObservableProperty] public partial string? ItemStepType { get; set; }
    [ObservableProperty] public partial Item? ItemStepSelected { get; set; }
    public bool CanConfirmItem => ItemStepSelected is not null;
    partial void OnItemStepSelectedChanged(Item? value) => OnPropertyChanged(nameof(CanConfirmItem));

    partial void OnItemStepTypeChanged(string? value) => _ = LoadItemStepOptions();
    private async Task LoadItemStepOptions()
    {
        ItemStepOptions.Clear();
        ItemStepSelected = null;
        if (string.IsNullOrWhiteSpace(ItemStepType)) return;
        var items = await _inventory.GetItemsAsync(type: ItemStepType);
        foreach (var it in items) ItemStepOptions.Add(it);
    }

    [RelayCommand]
    private async Task StartAddItemUsed()
    {
        if (ItemStepTypes.Count == 0)
            foreach (var t in await _service.GetTypeOptionsAsync()) ItemStepTypes.Add(t);
        ItemStepType = null; ItemStepSelected = null;
        ItemStepActive = true; ItemMorePrompt = false;
    }

    [RelayCommand]
    private void ConfirmAddItemUsed()
    {
        if (ItemStepSelected is null) return;
        if (AddItemTags.All(t => t.Id != ItemStepSelected.Id))
            AddItemTags.Add(new ItemTag { Id = ItemStepSelected.Id, Name = ItemStepSelected.Name });
        ItemStepActive = false; ItemMorePrompt = true;
    }

    [RelayCommand]
    private void AddMoreItems() { _ = StartAddItemUsed(); }

    [RelayCommand]
    private void FinishAddItems() { ItemStepActive = false; ItemMorePrompt = false; }

    [RelayCommand]
    private void CancelAddItemUsed() { ItemStepActive = false; ItemMorePrompt = false; ItemStepSelected = null; ItemStepType = null; }

    [RelayCommand]
    private void RemoveItemTag(ItemTag tag) => AddItemTags.Remove(tag);

    // ═════════════════════════ BOARD CREATE / EDIT ═════════════════════════

    private int _editingBoardId;
    [ObservableProperty] public partial bool IsBoardModalOpen { get; set; }
    [ObservableProperty] public partial string BoardModalTitle { get; set; } = "Create New Board";
    [ObservableProperty] public partial string? NewBoardName { get; set; }
    [ObservableProperty] public partial string? NewBoardDescription { get; set; }
    [ObservableProperty] public partial bool NewBoardSetDefaults { get; set; }
    [ObservableProperty] public partial string? NewBoardDefaultSentiment { get; set; }

    public ObservableCollection<CheckOption> BoardDefaultTypeOptions { get; } = new();
    public ObservableCollection<CheckOption> BoardDefaultThemeOptions { get; } = new();
    public ObservableCollection<CheckOption> BoardDefaultColorOptions { get; } = new();
    [ObservableProperty] public partial bool BoardDefTypeOpen { get; set; }
    [ObservableProperty] public partial bool BoardDefThemeOpen { get; set; }
    [ObservableProperty] public partial bool BoardDefColorOpen { get; set; }
    [ObservableProperty] public partial string BoardDefTypesDisplay { get; set; } = "Select default types...";
    [ObservableProperty] public partial string BoardDefThemesDisplay { get; set; } = "Select default themes...";
    [ObservableProperty] public partial string BoardDefColorsDisplay { get; set; } = "Select default colors...";

    [RelayCommand] private void ToggleBoardDefType() => BoardDefTypeOpen = !BoardDefTypeOpen;
    [RelayCommand] private void ToggleBoardDefTheme() => BoardDefThemeOpen = !BoardDefThemeOpen;
    [RelayCommand] private void ToggleBoardDefColor() => BoardDefColorOpen = !BoardDefColorOpen;
    [RelayCommand] private void ToggleBoardDefTypeOption(CheckOption o) { o.IsSelected = !o.IsSelected; BoardDefTypesDisplay = Join(BoardDefaultTypeOptions, "Select default types..."); }
    [RelayCommand] private void ToggleBoardDefThemeOption(CheckOption o) { o.IsSelected = !o.IsSelected; BoardDefThemesDisplay = Join(BoardDefaultThemeOptions, "Select default themes..."); }
    [RelayCommand] private void ToggleBoardDefColorOption(CheckOption o) { o.IsSelected = !o.IsSelected; BoardDefColorsDisplay = Join(BoardDefaultColorOptions, "Select default colors..."); }

    private void SeedBoardDefaultPickers(string? types, string? themes, string? colors)
    {
        BoardDefaultTypeOptions.Clear(); BoardDefaultThemeOptions.Clear(); BoardDefaultColorOptions.Clear();
        var t = Split(types); var th = Split(themes); var c = Split(colors);
        foreach (var x in TypeOptions) BoardDefaultTypeOptions.Add(new CheckOption(x) { IsSelected = t.Contains(x, StringComparer.OrdinalIgnoreCase) });
        foreach (var x in ThemeOptions) BoardDefaultThemeOptions.Add(new CheckOption(x) { IsSelected = th.Contains(x, StringComparer.OrdinalIgnoreCase) });
        foreach (var x in InspirationService.ColorOptions) BoardDefaultColorOptions.Add(new CheckOption(x) { IsSelected = c.Contains(x, StringComparer.OrdinalIgnoreCase) });
        BoardDefTypesDisplay = Join(BoardDefaultTypeOptions, "Select default types...");
        BoardDefThemesDisplay = Join(BoardDefaultThemeOptions, "Select default themes...");
        BoardDefColorsDisplay = Join(BoardDefaultColorOptions, "Select default colors...");
    }

    private static List<string> Split(string? s) => string.IsNullOrWhiteSpace(s)
        ? new List<string>()
        : s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    [RelayCommand]
    private void StartCreateBoard()
    {
        _editingBoardId = 0;
        BoardModalTitle = "Create New Board";
        NewBoardName = null; NewBoardDescription = null; NewBoardSetDefaults = false;
        NewBoardDefaultSentiment = null;
        SeedBoardDefaultPickers(null, null, null);
        BoardDefTypeOpen = BoardDefThemeOpen = BoardDefColorOpen = false;
        IsBoardModalOpen = true;
    }

    [RelayCommand]
    private async Task StartEditBoard(BoardCard card)
    {
        var b = await _service.GetBoardAsync(card.Id);
        if (b is null) return;
        _editingBoardId = b.Id;
        BoardModalTitle = "Edit Board";
        NewBoardName = b.Name;
        NewBoardDescription = b.Description;
        NewBoardDefaultSentiment = b.DefaultSentiment;
        NewBoardSetDefaults = b.HasDefaults;
        SeedBoardDefaultPickers(b.DefaultTypes, b.DefaultThemes, b.DefaultColors);
        BoardDefTypeOpen = BoardDefThemeOpen = BoardDefColorOpen = false;
        IsBoardModalOpen = true;
    }

    [RelayCommand]
    private void CancelBoard() => IsBoardModalOpen = false;

    [RelayCommand]
    private async Task SaveBoard()
    {
        if (string.IsNullOrWhiteSpace(NewBoardName)) return;

        string? types = null, themes = null, colors = null, sentiment = null;
        if (NewBoardSetDefaults)
        {
            types = J(BoardDefaultTypeOptions);
            themes = J(BoardDefaultThemeOptions);
            colors = J(BoardDefaultColorOptions);
            sentiment = string.IsNullOrWhiteSpace(NewBoardDefaultSentiment) ? null : NewBoardDefaultSentiment.Trim();
        }

        if (_editingBoardId > 0)
        {
            var b = await _service.GetBoardAsync(_editingBoardId);
            if (b is not null)
            {
                b.Name = NewBoardName.Trim();
                b.Description = string.IsNullOrWhiteSpace(NewBoardDescription) ? null : NewBoardDescription.Trim();
                b.DefaultTypes = types; b.DefaultThemes = themes; b.DefaultColors = colors; b.DefaultSentiment = sentiment;
                await _service.UpdateBoardAsync(b);
            }
        }
        else
        {
            await _service.CreateBoardAsync(new InspirationBoard
            {
                Name = NewBoardName.Trim(),
                Description = string.IsNullOrWhiteSpace(NewBoardDescription) ? null : NewBoardDescription.Trim(),
                ParentBoardId = _currentBoardId,
                DefaultTypes = types, DefaultThemes = themes, DefaultColors = colors, DefaultSentiment = sentiment,
            });
        }

        IsBoardModalOpen = false;
        await Load();
    }

    [RelayCommand]
    private async Task DeleteBoard(BoardCard card)
    {
        var page = Application.Current?.Windows[0].Page;
        bool ok = page is not null && await page.DisplayAlert("Delete Board",
            $"Delete board \"{card.Name}\"?\n\nImages in this board will be moved to the parent level. Sub-boards will be promoted to the parent level.",
            "Delete", "Cancel");
        if (!ok) return;
        await _service.DeleteBoardAsync(card.Id);
        await Load();
    }

    // ═════════════════════════ IMAGE DETAIL POPUP ═════════════════════════

    private int _detailImageId;
    [ObservableProperty] public partial bool IsDetailOpen { get; set; }
    [ObservableProperty] public partial string? DetailImageUrl { get; set; }
    [ObservableProperty] public partial string? DetailTitle { get; set; }
    [ObservableProperty] public partial string? DetailDate { get; set; }
    [ObservableProperty] public partial string? DetailColor { get; set; }
    [ObservableProperty] public partial string? DetailTeColor { get; set; }
    [ObservableProperty] public partial string? DetailTypes { get; set; }
    [ObservableProperty] public partial string? DetailTheme { get; set; }
    [ObservableProperty] public partial string? DetailSentiment { get; set; }
    [ObservableProperty] public partial string? DetailNotes { get; set; }
    public ObservableCollection<LinkedItemRow> DetailItems { get; } = new();
    [ObservableProperty] public partial bool DetailHasItems { get; set; }

    [RelayCommand]
    private async Task OpenImage(ImageCard card)
    {
        var img = await _service.GetAsync(card.Id);
        if (img is null) return;
        _detailImageId = img.Id;
        DetailImageUrl = img.ImageUrl;
        DetailTitle = string.IsNullOrEmpty(img.Title) ? "Inspiration" : img.Title;
        DetailDate = img.CreatedAt.ToString("MMMM d, yyyy");
        DetailColor = img.Color; DetailTeColor = img.TeColor;
        DetailTypes = img.Types; DetailTheme = img.Theme;
        DetailSentiment = img.Sentiment; DetailNotes = img.Notes;

        DetailItems.Clear();
        foreach (var it in await _service.GetLinkedItemsAsync(img.Id))
        {
            var meta = it.Type;
            if (!string.IsNullOrEmpty(it.ItemNumber)) meta += $" · #{it.ItemNumber}";
            DetailItems.Add(new LinkedItemRow { Id = it.Id, Name = it.Name, Meta = meta });
        }
        DetailHasItems = DetailItems.Count > 0;
        IsDetailOpen = true;
    }

    [RelayCommand]
    private void CloseDetail() => IsDetailOpen = false;

    [RelayCommand]
    private void OpenLinkedItem(LinkedItemRow row)
    {
        IsDetailOpen = false;
        _nav.PushDetail(row.Id);
    }

    [RelayCommand]
    private async Task DeleteImage()
    {
        if (_detailImageId <= 0) return;
        var page = Application.Current?.Windows[0].Page;
        var label = string.IsNullOrEmpty(DetailTitle) || DetailTitle == "Inspiration" ? "this photo" : $"\"{DetailTitle}\"";
        bool ok = page is not null && await page.DisplayAlert("Delete Photo",
            $"Permanently delete {label}? This cannot be undone.", "Delete", "Cancel");
        if (!ok) return;
        await _service.DeleteAsync(_detailImageId);
        IsDetailOpen = false;
        await Load();
    }

    // Move the detail image to another board
    public ObservableCollection<CheckOption> MoveTargets { get; } = new();
    [ObservableProperty] public partial bool IsMoveOpen { get; set; }

    [RelayCommand]
    private async Task StartMoveImage()
    {
        MoveTargets.Clear();
        MoveTargets.Add(new CheckOption("Uncategorized (no board)", -1));
        foreach (var b in await _service.GetAllBoardsFlatAsync())
            MoveTargets.Add(new CheckOption(b.Name, b.Id));
        IsMoveOpen = true;
    }

    [RelayCommand]
    private async Task MoveImageTo(CheckOption target)
    {
        int tag = (int)target.Tag!;
        await _service.MoveImageToBoardAsync(_detailImageId, tag < 0 ? null : tag);
        IsMoveOpen = false;
        IsDetailOpen = false;
        await Load();
    }

    [RelayCommand]
    private void CancelMove() => IsMoveOpen = false;

    // ═════════════════════════ ORGANIZE / BULK MOVE ═════════════════════════

    [ObservableProperty] public partial bool IsOrganizing { get; set; }
    [ObservableProperty] public partial string OrgSelectedLabel { get; set; } = "0 selected";
    public ObservableCollection<CheckOption> OrgMoveTargets { get; } = new();
    [ObservableProperty] public partial CheckOption? OrgMoveTarget { get; set; }

    [RelayCommand]
    private async Task ToggleOrganizing()
    {
        IsOrganizing = !IsOrganizing;
        if (IsOrganizing)
        {
            OrgMoveTargets.Clear();
            OrgMoveTargets.Add(new CheckOption("Uncategorized (no board)", -1));
            foreach (var b in await _service.GetAllBoardsFlatAsync())
                OrgMoveTargets.Add(new CheckOption(b.Name, b.Id));
            foreach (var img in Images) img.IsOrgSelected = false;
            UpdateOrgLabel();
        }
    }

    [RelayCommand]
    private void ToggleOrgImage(ImageCard card) { card.IsOrgSelected = !card.IsOrgSelected; UpdateOrgLabel(); }

    [RelayCommand]
    private void SelectAllOrg() { foreach (var i in Images) i.IsOrgSelected = true; UpdateOrgLabel(); }

    [RelayCommand]
    private void ClearOrg() { foreach (var i in Images) i.IsOrgSelected = false; UpdateOrgLabel(); }

    private void UpdateOrgLabel() => OrgSelectedLabel = $"{Images.Count(i => i.IsOrgSelected)} selected";

    [RelayCommand]
    private async Task BulkMove()
    {
        var selected = Images.Where(i => i.IsOrgSelected).Select(i => i.Id).ToList();
        if (selected.Count == 0 || OrgMoveTarget is null) return;
        int tag = (int)OrgMoveTarget.Tag!;
        foreach (var id in selected)
            await _service.MoveImageToBoardAsync(id, tag < 0 ? null : tag);
        IsOrganizing = false;
        await Load();
    }
}
