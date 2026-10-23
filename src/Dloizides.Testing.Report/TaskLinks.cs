using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal sealed class TaskLinks
{
    public const string IdToken = "{id}";

    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    public static readonly TaskLinks None = new(null, string.Empty);

    private readonly Regex? pattern;

    private TaskLinks(Regex? pattern, string urlTemplate)
    {
        this.pattern = pattern;
        UrlTemplate = urlTemplate;
    }

    public string UrlTemplate { get; }

    public static TaskLinks Create(string pattern, string urlTemplate) =>
        new(new Regex(pattern, RegexOptions.CultureInvariant, MatchTimeout), urlTemplate);

    public static bool IsValidPattern(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.CultureInvariant, MatchTimeout);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public IReadOnlyList<string> Of(TestResult test) =>
        pattern is null
            ? []
            : pattern.Matches($"{test.Description} {test.Name}").Select(m => m.Value).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

    public string Url(string id) =>
        UrlTemplate.Length == 0 ? string.Empty : UrlTemplate.Replace(IdToken, Uri.EscapeDataString(id), StringComparison.Ordinal);

    public string Link(string id, string css)
    {
        var url = Url(id);
        var text = Html.Encode(id);
        return url.Length == 0
            ? $"<span class=\"{css}\">{text}</span>"
            : $"<a class=\"{css}\" href=\"{Html.Encode(url)}\" target=\"_blank\" rel=\"noopener\">{text}</a>";
    }

    public string Chips(IReadOnlyList<string> ids) =>
        ids.Count == 0 ? string.Empty : $"<span class=\"tasks\">{string.Concat(ids.Select(i => Link(i, "tid")))}</span>";
}
