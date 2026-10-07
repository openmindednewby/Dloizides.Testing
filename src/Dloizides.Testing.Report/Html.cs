using System.Globalization;
using System.Net;

namespace Dloizides.Testing.Report;

internal static class Html
{
    private const int SecondsPerMinute = 60;
    private const int MillisecondsPerSecond = 1000;

    public static string Encode(string text) => WebUtility.HtmlEncode(text);

    public static string Page(string title, string body, bool withScript)
    {
        var script = withScript ? $"<script>{Assets.Js}</script>" : string.Empty;
        return "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">"
            + "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
            + $"<title>{Encode(title)}</title><style>{Assets.Css}</style></head>"
            + $"<body><div class=\"wrap\">{body}</div>{script}</body></html>";
    }

    public static string Duration(double seconds)
    {
        var culture = CultureInfo.InvariantCulture;
        if (seconds < 1)
            return string.Format(culture, "{0:0} ms", seconds * MillisecondsPerSecond);
        if (seconds < SecondsPerMinute)
            return string.Format(culture, "{0:0.0} s", seconds);
        return string.Format(culture, "{0} min {1} s", Math.Floor(seconds / SecondsPerMinute), Math.Round(seconds % SecondsPerMinute));
    }

    public static string RunDate(TestRun run) =>
        run.Date?.ToString("dddd d MMMM yyyy, HH:mm", CultureInfo.InvariantCulture) ?? run.Name;

    public static string TallyBar(Tally tally, string extraClass)
    {
        var segments = string.Concat(StatusText.BarOrder
            .Where(s => tally[s] > 0)
            .Select(s => $"<span class=\"seg {StatusText.Css(s)}\" style=\"flex-grow:{tally[s]}\"></span>"));
        return $"<div class=\"bar {extraClass}\" role=\"img\" aria-label=\"{Encode(tally.Text())}\">{segments}</div>";
    }
}

internal static class Assets
{
    public static readonly string Css = Load("report.css");

    public static readonly string Js = Load("report.js");

    private static string Load(string name)
    {
        using var stream = typeof(Assets).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded asset {name} is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
