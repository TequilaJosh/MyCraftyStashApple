using System.Globalization;

namespace MyCraftyStash.Converters;

/// <summary>True when the value is non-null (and, for strings, non-empty).</summary>
public class IsNotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string s ? !string.IsNullOrEmpty(s) : value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not true;
}

/// <summary>
/// Turns MCS's stored image string into a MAUI ImageSource. The desktop app
/// stores photos as base64 (often a "data:image/...;base64,XXXX" data URI)
/// inline in image_url; older/imported rows may hold an http(s) URL or a file
/// path. Handles all three; returns null (blank) when empty or unparseable.
/// </summary>
public class ImageStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
            return null;

        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return ImageSource.FromUri(new Uri(s));

        // Strip a data-URI prefix if present, then treat the rest as base64.
        var b64 = s;
        int comma = b64.IndexOf(',');
        if (b64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
            b64 = b64[(comma + 1)..];

        try
        {
            var bytes = System.Convert.FromBase64String(b64);
            return ImageSource.FromStream(() => new MemoryStream(bytes));
        }
        catch (FormatException)
        {
            // Not base64 — assume a local file path.
            return ImageSource.FromFile(s);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
