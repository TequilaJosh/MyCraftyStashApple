namespace MyCraftyStash.Views;

public partial class ComingSoonView : ContentView
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

    public ComingSoonView()
    {
        InitializeComponent();
    }

    public void SetSection(string route)
    {
        if (Sections.TryGetValue(route, out var s))
        {
            TitleLabel.Text = s.Title;
            IconLabel.Text = s.Icon;
        }
    }
}
