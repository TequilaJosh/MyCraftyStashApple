using CommunityToolkit.Mvvm.ComponentModel;

namespace MyCraftyStash.ViewModels;

/// <summary>
/// Envelope &amp; Box calculator — ported verbatim from the desktop's math.
/// Given a card size (and box depth), computes the paper size and score lines
/// for a diagonal-fold envelope or a box. Recalculates as inputs change.
/// </summary>
public partial class EnvelopeExpertViewModel : ObservableObject
{
    private const float EnvelopeMargin = 0.56125f;
    private const float BoxMargin = 0.53125f;

    public List<string> Fractions { get; } = new()
        { "0", "1/8", "1/4", "3/8", "1/2", "5/8", "3/4", "7/8" };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeightLabel))]
    public partial bool IsBoxMode { get; set; }

    public string HeightLabel => IsBoxMode ? "Length" : "Height";

    [ObservableProperty] public partial string WidthWhole { get; set; } = "";
    [ObservableProperty] public partial string WidthFraction { get; set; } = "0";
    [ObservableProperty] public partial string HeightWhole { get; set; } = "";
    [ObservableProperty] public partial string HeightFraction { get; set; } = "0";
    [ObservableProperty] public partial string DepthWhole { get; set; } = "";
    [ObservableProperty] public partial string DepthFraction { get; set; } = "0";

    [ObservableProperty] public partial string? Error { get; set; }
    [ObservableProperty] public partial bool HasResults { get; set; }
    [ObservableProperty] public partial string PaperSize { get; set; } = "—";
    [ObservableProperty] public partial string ScoreLine { get; set; } = "—";
    [ObservableProperty] public partial string BoxFirstScore { get; set; } = "—";
    [ObservableProperty] public partial string BoxSecondScore { get; set; } = "—";

    partial void OnIsBoxModeChanged(bool value) => Calculate();
    partial void OnWidthWholeChanged(string value) => Calculate();
    partial void OnWidthFractionChanged(string value) => Calculate();
    partial void OnHeightWholeChanged(string value) => Calculate();
    partial void OnHeightFractionChanged(string value) => Calculate();
    partial void OnDepthWholeChanged(string value) => Calculate();
    partial void OnDepthFractionChanged(string value) => Calculate();

    private void Calculate()
    {
        Error = null;
        HasResults = false;

        if (!TryParse(WidthWhole, WidthFraction, out float width)) return;
        if (!TryParse(HeightWhole, HeightFraction, out float height)) return;
        if (width <= 0 || height <= 0) return;

        if (IsBoxMode)
        {
            if (!TryParse(DepthWhole, DepthFraction, out float depth)) return;
            if (depth <= 0) return;
            CalculateBox(width, height, depth);
        }
        else
        {
            CalculateEnvelope(width, height);
        }
        HasResults = true;
    }

    private void CalculateEnvelope(float width, float height)
    {
        float length = Math.Max(width, height);
        float w = Math.Min(width, height);
        double calcLength = length * Math.Sqrt(0.5);
        double calcWidth = w * Math.Sqrt(0.5);
        double paperSize = calcWidth + calcLength + (EnvelopeMargin * 2);
        double scoreLine = calcLength + EnvelopeMargin;
        PaperSize = ToFractionString(paperSize, 8) + " inches";
        ScoreLine = ToFractionString(scoreLine, 8) + " inches";
    }

    private void CalculateBox(float width, float length, float depth)
    {
        double calcLength = length * Math.Sqrt(0.5);
        double calcWidth = width * Math.Sqrt(0.5);
        double calcDepth = depth * Math.Sqrt(0.5);
        double paperSize = calcWidth + calcLength + (2 * (calcDepth + BoxMargin));
        double firstScore = BoxMargin + calcWidth;
        double secondScore = (BoxMargin + calcWidth) + (2 * calcDepth);
        PaperSize = ToFractionString(paperSize, 8) + " inches";
        BoxFirstScore = ToFractionString(firstScore, 8) + " inches";
        BoxSecondScore = ToFractionString(secondScore, 8) + " inches";
    }

    private bool TryParse(string wholeText, string fraction, out float result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(wholeText)) return false;
        if (!int.TryParse(wholeText.Trim(), out int whole) || whole < 0)
        {
            Error = "Please enter valid whole numbers for dimensions.";
            return false;
        }
        result = whole + FractionToDecimal(fraction);
        return true;
    }

    private static float FractionToDecimal(string fraction) => fraction switch
    {
        "1/8" => 0.125f, "1/4" => 0.25f, "3/8" => 0.375f,
        "1/2" => 0.50f, "5/8" => 0.625f, "3/4" => 0.75f,
        "7/8" => 0.875f, _ => 0f
    };

    private static string ToFractionString(double value, int denominator)
    {
        int whole = (int)Math.Floor(value);
        double rem = value - whole;
        int numerator = (int)Math.Round(rem * denominator);
        int g = GCD(Math.Abs(numerator), denominator);
        if (g == 0) g = 1;
        numerator /= g;
        int denom = denominator / g;
        if (numerator == denom) return $"{whole + 1}";
        if (numerator == 0) return $"{whole}";
        if (whole == 0) return $"{numerator}/{denom}";
        return $"{whole} {numerator}/{denom}";
    }

    private static int GCD(int a, int b) => b == 0 ? a : GCD(b, a % b);
}
