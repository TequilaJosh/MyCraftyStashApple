using Microsoft.Extensions.DependencyInjection;
using MyCraftyStash.ViewModels;
using MyCraftyStash.Views;

namespace MyCraftyStash.Services;

/// <summary>Implemented by a section VM that should reload when a pushed
/// sub-view (detail/edit) is popped back to it.</summary>
public interface IRefreshOnReturn
{
    Task Refresh();
}

/// <summary>
/// App-wide content navigator. The sidebar stays put in a fixed Grid column;
/// this swaps the view shown in the content pane on the right, with a simple
/// back stack for the inventory → detail → edit flow. This replaces MAUI
/// Shell (whose Windows flyout kept collapsing the sidebar) with the desktop
/// app's own pattern: a persistent sidebar + a swapped content region.
/// </summary>
public class AppNavigator
{
    private readonly IServiceProvider _sp;
    private readonly Stack<View> _stack = new();

    public AppNavigator(IServiceProvider sp) => _sp = sp;

    public event Action? Changed;
    public string CurrentRoute { get; private set; } = "home";
    public View? Current => _stack.Count > 0 ? _stack.Peek() : null;

    /// <summary>Top-level nav from the sidebar: resets the stack to the section.</summary>
    public void ShowSection(string route)
    {
        CurrentRoute = route;
        _stack.Clear();
        _stack.Push(BuildSection(route));
        Changed?.Invoke();
    }

    public void PushAddItem() => Push(_sp.GetRequiredService<ItemEditView>());

    public void PushEditItem(int itemId)
    {
        var view = _sp.GetRequiredService<ItemEditView>();
        (view.BindingContext as ItemEditViewModel)?.Init(itemId);
        Push(view);
    }

    public void PushDetail(int itemId)
    {
        var view = _sp.GetRequiredService<ItemDetailView>();
        (view.BindingContext as ItemDetailViewModel)?.Init(itemId);
        Push(view);
    }

    /// <summary>Wish-list add (id 0) or edit form.</summary>
    public void PushWishlistEdit(int id)
    {
        var view = _sp.GetRequiredService<WishlistEditView>();
        (view.BindingContext as WishlistEditViewModel)?.Init(id);
        Push(view);
    }

    public void PushInspirationDetail(int id)
    {
        var view = _sp.GetRequiredService<InspirationDetailView>();
        (view.BindingContext as InspirationDetailViewModel)?.Init(id);
        Push(view);
    }

    public void PushProjectDetail(int id)
    {
        var view = _sp.GetRequiredService<ProjectDetailView>();
        (view.BindingContext as ProjectDetailViewModel)?.Init(id);
        Push(view);
    }

    /// <summary>Project add (id 0) or edit form.</summary>
    public void PushProjectEdit(int id)
    {
        var view = _sp.GetRequiredService<ProjectEditView>();
        (view.BindingContext as ProjectEditViewModel)?.Init(id);
        Push(view);
    }

    public async void Back()
    {
        if (_stack.Count <= 1) return;
        _stack.Pop();
        Changed?.Invoke();
        if (Current?.BindingContext is IRefreshOnReturn r)
            await r.Refresh();
    }

    private void Push(View view)
    {
        _stack.Push(view);
        Changed?.Invoke();
    }

    private View BuildSection(string route) => route switch
    {
        "home" => _sp.GetRequiredService<HomeView>(),
        "inventory" => _sp.GetRequiredService<InventoryView>(),
        "wishlist" => _sp.GetRequiredService<WishlistView>(),
        "stocktracker" => _sp.GetRequiredService<StockTrackerView>(),
        "sentiment" => _sp.GetRequiredService<SentimentSearchView>(),
        "inspiration" => _sp.GetRequiredService<InspirationView>(),
        "projects" => _sp.GetRequiredService<ProjectsView>(),
        "envelope" => _sp.GetRequiredService<EnvelopeExpertView>(),
        "expense" => Report(ReportKind.Expense),
        "sales" => Report(ReportKind.Sales),
        "settings" => _sp.GetRequiredService<SettingsView>(),
        _ => ComingSoon(route),
    };

    private View ComingSoon(string route)
    {
        var view = _sp.GetRequiredService<ComingSoonView>();
        view.SetSection(route);
        return view;
    }

    private View Report(ReportKind kind)
    {
        var view = _sp.GetRequiredService<ReportView>();
        view.SetKind(kind);
        return view;
    }
}
