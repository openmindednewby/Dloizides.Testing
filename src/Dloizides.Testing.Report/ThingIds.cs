namespace Dloizides.Testing.Report;

internal sealed class ThingIds
{
    private readonly Dictionary<(string Project, string Class), string> ids = [];

    public static ThingIds For(IEnumerable<TestResult> tests)
    {
        var things = new ThingIds();
        var used = new HashSet<string>(StringComparer.Ordinal);
        var keys = tests.Select(t => (t.Project, t.Class)).Distinct()
            .OrderBy(k => k.Project, StringComparer.Ordinal).ThenBy(k => k.Class, StringComparer.Ordinal);
        foreach (var key in keys)
        {
            var root = Root(key.Project, key.Class);
            var id = root;
            for (var n = 2; !used.Add(id); n++)
                id = $"{root}-{n}";
            things.ids[key] = id;
        }

        return things;
    }

    public string Of(TestResult test) =>
        ids.TryGetValue((test.Project, test.Class), out var id) ? id : Root(test.Project, test.Class);

    private static string Root(string project, string className) => $"c-{MermaidText.Slug(project)}-{MermaidText.Slug(className)}";
}
