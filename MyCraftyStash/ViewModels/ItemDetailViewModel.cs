using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>Rich item detail: image gallery, all fields, related items,
/// purchase/sales history, and sentiments — matching the desktop.</summary>
public partial class ItemDetailViewModel : ObservableObject, IRefreshOnReturn
{
    private readonly InventoryService _service;
    private readonly AppNavigator _nav;
    private int _id;

    public ItemDetailViewModel(InventoryService service, AppNavigator nav)
    {
        _service = service;
        _nav = nav;
    }

    [ObservableProperty] public partial Item? Item { get; set; }

    // Gallery
    public ObservableCollection<ItemImage> Images { get; } = new();
    [ObservableProperty] public partial bool HasImages { get; set; }

    // Related items
    public ObservableCollection<Item> RelatedItems { get; } = new();
    [ObservableProperty] public partial bool HasRelated { get; set; }
    [ObservableProperty] public partial string? RelatedSearch { get; set; }
    public ObservableCollection<Item> RelatedCandidates { get; } = new();

    // Purchases
    public ObservableCollection<ItemPurchase> Purchases { get; } = new();
    [ObservableProperty] public partial bool HasPurchases { get; set; }
    [ObservableProperty] public partial string PurchaseTotals { get; set; } = "";
    [ObservableProperty] public partial bool AddingPurchase { get; set; }
    [ObservableProperty] public partial string? PurchaseQty { get; set; }
    [ObservableProperty] public partial string? PurchasePrice { get; set; }

    // Sales
    public ObservableCollection<ItemSale> Sales { get; } = new();
    [ObservableProperty] public partial bool HasSales { get; set; }
    [ObservableProperty] public partial string SaleTotals { get; set; } = "";
    [ObservableProperty] public partial bool AddingSale { get; set; }
    [ObservableProperty] public partial string? SaleQty { get; set; }
    [ObservableProperty] public partial string? SalePrice { get; set; }

    // Sentiments
    public ObservableCollection<SentimentImage> Sentiments { get; } = new();
    [ObservableProperty] public partial bool HasSentiments { get; set; }
    [ObservableProperty] public partial string? NewSentiment { get; set; }

    public void Init(int id)
    {
        _id = id;
        _ = Refresh();
    }

    public async Task Refresh()
    {
        if (_id <= 0) return;
        Item = await _service.GetItemAsync(_id);

        Images.Clear();
        foreach (var img in await _service.GetItemImagesAsync(_id)) Images.Add(img);
        HasImages = Images.Count > 0;

        RelatedItems.Clear();
        foreach (var r in await _service.GetRelatedItemsAsync(_id)) RelatedItems.Add(r);
        HasRelated = RelatedItems.Count > 0;

        Purchases.Clear();
        foreach (var p in await _service.GetPurchasesAsync(_id)) Purchases.Add(p);
        HasPurchases = Purchases.Count > 0;
        var pq = Purchases.Sum(p => p.Quantity);
        var ps = Purchases.Sum(p => p.TotalPrice);
        PurchaseTotals = HasPurchases ? $"{pq} bought · {ps:C} spent" : "";

        Sales.Clear();
        foreach (var s in await _service.GetSalesAsync(_id)) Sales.Add(s);
        HasSales = Sales.Count > 0;
        var sq = Sales.Sum(s => s.Quantity);
        var sr = Sales.Sum(s => s.TotalPrice);
        SaleTotals = HasSales ? $"{sq} sold · {sr:C} earned" : "";

        Sentiments.Clear();
        foreach (var se in await _service.GetSentimentsAsync(_id)) Sentiments.Add(se);
        HasSentiments = Sentiments.Count > 0;
    }

    // ── Item-level ──
    [RelayCommand] private void Edit() { if (Item is not null) _nav.PushEditItem(Item.Id); }
    [RelayCommand] private void Back() => _nav.Back();
    [RelayCommand] private void OpenRelated(Item item) => _nav.PushDetail(item.Id);

    [RelayCommand]
    private async Task Delete()
    {
        if (Item is null) return;
        var page = Application.Current?.Windows[0].Page;
        bool ok = page is not null && await page.DisplayAlert("Delete item",
            $"Delete \"{Item.Name}\"? This can't be undone.", "Delete", "Cancel");
        if (!ok) return;
        await _service.DeleteItemAsync(Item.Id);
        _nav.Back();
    }

    // ── Images ──
    [RelayCommand]
    private async Task AddImage()
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync();
            if (photo is null) return;
            using var stream = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            await _service.AddItemImageAsync(_id, "data:image/jpeg;base64," + Convert.ToBase64String(ms.ToArray()));
            await Refresh();
        }
        catch { /* picker unavailable / denied */ }
    }

    [RelayCommand]
    private async Task DeleteImage(ItemImage img)
    {
        await _service.DeleteItemImageAsync(img.Id);
        await Refresh();
    }

    // ── Related ──
    [RelayCommand]
    private async Task SearchRelated()
    {
        var items = await _service.GetItemsAsync(RelatedSearch);
        RelatedCandidates.Clear();
        foreach (var i in items.Where(i => i.Id != _id).Take(12)) RelatedCandidates.Add(i);
    }

    [RelayCommand]
    private async Task AddRelated(Item item)
    {
        await _service.AddRelatedItemAsync(_id, item.Id);
        RelatedCandidates.Clear();
        RelatedSearch = "";
        await Refresh();
    }

    [RelayCommand]
    private async Task RemoveRelated(Item item)
    {
        await _service.RemoveRelatedItemAsync(_id, item.Id);
        await Refresh();
    }

    // ── Purchases ──
    [RelayCommand] private void StartAddPurchase() { AddingPurchase = true; PurchaseQty = "1"; PurchasePrice = null; }
    [RelayCommand] private void CancelAddPurchase() => AddingPurchase = false;

    [RelayCommand]
    private async Task SavePurchase()
    {
        int qty = int.TryParse(PurchaseQty, out var q) ? q : 1;
        decimal price = decimal.TryParse(PurchasePrice, out var p) ? p : 0;
        await _service.AddPurchaseAsync(_id, qty, price, DateTime.Now);
        AddingPurchase = false;
        await Refresh();
    }

    [RelayCommand] private async Task DeletePurchase(ItemPurchase p) { await _service.DeletePurchaseAsync(p.Id); await Refresh(); }

    // ── Sales ──
    [RelayCommand] private void StartAddSale() { AddingSale = true; SaleQty = "1"; SalePrice = null; }
    [RelayCommand] private void CancelAddSale() => AddingSale = false;

    [RelayCommand]
    private async Task SaveSale()
    {
        int qty = int.TryParse(SaleQty, out var q) ? q : 1;
        decimal price = decimal.TryParse(SalePrice, out var p) ? p : 0;
        await _service.AddSaleAsync(_id, qty, price, DateTime.Now);
        AddingSale = false;
        await Refresh();
    }

    [RelayCommand] private async Task DeleteSale(ItemSale s) { await _service.DeleteSaleAsync(s.Id); await Refresh(); }

    // ── Sentiments ──
    [RelayCommand]
    private async Task AddSentiment()
    {
        if (string.IsNullOrWhiteSpace(NewSentiment)) return;
        await _service.AddSentimentAsync(_id, NewSentiment.Trim());
        NewSentiment = "";
        await Refresh();
    }

    [RelayCommand] private async Task DeleteSentiment(SentimentImage s) { await _service.DeleteSentimentAsync(s.Id); await Refresh(); }
}
