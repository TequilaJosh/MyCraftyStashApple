namespace MyCraftyStash.Views;

public partial class ComingSoonPage : ContentPage
{
    // route -> (friendly title, glyph). Mirrors the desktop's sidebar labels.
    private static readonly Dictionary<string, (string Title, string Icon)> Sections = new()
    {
        ["home"] = ("Home", "⌂"),
        ["sentiment"] = ("Sentiment Search", "❝"),
        ["inspiration"] = ("Inspiration", "✦"),
        ["wishlist"] = ("Wish List", "♡"),
        ["colormatch"] = ("Color Match", "▦"),
        ["envelope"] = ("Envelope & Box", "✉"),
        ["social"] = ("Social", "❀"),
        ["projects"] = ("Projects", "✿"),
        ["stocktracker"] = ("Stock Tracker", "◐"),
        ["expense"] = ("Expense Report", "$"),
        ["sales"] = ("Sales Report", "↗"),
        ["about"] = ("About", "ⓘ"),
    };

    public ComingSoonPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var loc = Shell.Current?.CurrentState?.Location?.OriginalString ?? "";
        var route = loc.TrimStart('/').Split('/').LastOrDefault() ?? "";
        if (Sections.TryGetValue(route, out var s))
        {
            Title = s.Title;
            TitleLabel.Text = s.Title;
            IconLabel.Text = s.Icon;
        }
    }
}
