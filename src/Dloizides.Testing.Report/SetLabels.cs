namespace Dloizides.Testing.Report;

internal sealed class SetLabels(IReadOnlyDictionary<string, string> overrides)
{
    private static readonly Dictionary<string, string> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Baseline"] = "Today's behaviour — must pass",
        ["MigrationTarget"] = "After the database merge — red until the merge is built",
        ["Live"] = "Calls real external services — off by default",
    };

    public static SetLabels None { get; } = new(new Dictionary<string, string>());

    public string For(string setName)
    {
        if (overrides.TryGetValue(setName, out var custom))
            return custom;
        return Defaults.TryGetValue(setName, out var fallback) ? fallback : string.Empty;
    }

    public string Span(string setName, string cssClass)
    {
        var label = For(setName);
        return label.Length == 0 ? string.Empty : $"<span class=\"{cssClass}\">{Html.Encode(label)}</span>";
    }
}
