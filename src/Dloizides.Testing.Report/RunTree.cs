using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record Fold(string Kind, string? Id, Tally? Status)
{
    public static readonly Fold Inner = new(string.Empty, null, null);

    public string Title { get; init; } = string.Empty;

    public string Tag { get; init; } = string.Empty;

    public IReadOnlyList<string> Path { get; init; } = [];

    public string Meta { get; init; } = string.Empty;

    public string Lead { get; init; } = string.Empty;
}

internal sealed class RunTree(RunDiagrams diagrams, ThingIds things, IReadOnlyList<ResultsFeature> features)
{
    private const string TableHead =
        "<colgroup><col class=\"c1\"><col class=\"c2\"><col class=\"c3\"><col class=\"c4\"></colgroup>"
        + "<thead><tr><th scope=\"col\">Scenario</th><th scope=\"col\">Expected</th><th scope=\"col\">Result</th><th scope=\"col\" class=\"num\">Time</th></tr></thead>";

    private const string MethodKind = "method";

    private const string Dot = " · ";

    private static readonly StringComparer Ordering = StringComparer.OrdinalIgnoreCase;

    private readonly RunRows rows = new();
    private readonly StringBuilder nav = new();
    private int areaCounter;

    public IReadOnlyList<(string Id, string Name)> Problems => rows.Problems;

    public int TestCount => rows.Count;

    public string Nav => nav.ToString();

    private static string E(string text) => Html.Encode(text);

    public string Render(IReadOnlyList<TestResult> tests, bool multiProject)
    {
        var builder = new StringBuilder();
        var areas = tests.GroupBy(t => t.Feature, StringComparer.Ordinal)
            .OrderBy(g => g.Min(t => (int)t.Status)).ThenBy(g => g.Key, Ordering);
        foreach (var area in areas)
            builder.Append(Area(area.Key, area.ToList(), multiProject));
        return builder.ToString();
    }

    private string Area(string feature, List<TestResult> tests, bool multiProject)
    {
        areaCounter++;
        var id = $"a-{areaCounter}-{MermaidText.Slug(feature)}";
        var tally = new Tally(tests);
        var declared = FeatureTitle.Find(features, feature);
        var title = declared?.Name ?? FeatureTitle.Words(feature);
        nav.Append($"<li><a href=\"#{id}\">{Badges.Dot(tally)}{E(title)}<span class=\"n\">{tests.Count}</span></a></li>");
        var classes = tests.GroupBy(t => (t.Project, t.Class))
            .OrderBy(g => g.Min(t => (int)t.Status)).ThenBy(g => g.Key.Class, Ordering)
            .Select(g => g.ToList()).ToList();
        var head = new Fold("area", id, tally) { Title = title, Lead = AreaLead(feature, declared, tests) };
        if (classes.Count == 1)
            return Thing(classes[0], head, multiProject);
        head = head with { Tag = $"Flow{Dot}{classes.Count} classes", Path = [string.Join(Dot, ClassNames(classes))] };
        var body = new StringBuilder(head.Lead);
        foreach (var thing in classes)
            body.Append(Thing(thing, Fold.Inner, multiProject));
        return Group(head, tests, body.ToString());
    }

    private static IEnumerable<string> ClassNames(List<List<TestResult>> classes) =>
        classes.Select(c => FeatureTitle.Subject(c[0].Class)).Distinct(StringComparer.Ordinal).Order(Ordering);

    private string AreaLead(string feature, ResultsFeature? declared, List<TestResult> tests)
    {
        var classes = tests.Select(t => t.Class).ToHashSet(StringComparer.Ordinal);
        var flows = diagrams.Flows.Where(f => f.Steps.Any(s => s.Things.Any(classes.Contains))).ToList();
        var parts = flows.SelectMany(f => f.Steps.Where(s => s.Things.Any(classes.Contains))
            .Select(s => $"Part of: <a href=\"#{E(f.Anchor)}\">{E(f.Name)}</a>, step {s.Step} of {f.LastStep}")).ToList();
        var schema = diagrams.Schema is not null && classes.Overlaps(diagrams.SchemaClasses)
            ? "<p class=\"partof\"><a href=\"#schema-h\">Database diagram</a></p>"
            : string.Empty;
        return AreaSections.Why(declared)
            + AreaSections.UseCases(diagrams.UseCases.FirstOrDefault(d => d.Area == feature))
            + string.Concat(parts.Select(p => $"<p class=\"partof\">{p}</p>"))
            + AreaSections.Flows(flows)
            + AreaSections.Sequence(diagrams.Sequences.FirstOrDefault(d => d.Area == feature))
            + schema;
    }

    private string Thing(List<TestResult> tests, Fold fold, bool multiProject)
    {
        var first = tests[0];
        var thingId = things.Of(first);
        var project = multiProject ? $"<span class=\"proj\">{E(first.Project)}</span>" : string.Empty;
        var anchor = fold.Id is null ? string.Empty : $"<span class=\"anc\" id=\"{E(thingId)}\"></span>";
        var head = fold with
        {
            Kind = fold.Id is null ? "thing" : fold.Kind,
            Id = fold.Id ?? thingId,
            Title = fold.Title.Length > 0 ? fold.Title : FeatureTitle.Class(first.Class),
            Tag = FeatureTitle.Kind(first.Class),
            Path = [FeatureTitle.Bare(first.Class)],
            Meta = fold.Meta + project + anchor + Feeds(FlowSteps(first.Class)),
            Lead = fold.Lead + RequirementFigure(thingId),
        };
        var methods = tests.GroupBy(t => t.Method, StringComparer.Ordinal)
            .OrderBy(g => g.Min(t => (int)t.Status)).ThenBy(g => g.Key, Ordering)
            .Select(g => g.ToList()).ToList();
        if (methods.Count == 1)
            return Method(methods[0], head);
        var body = new StringBuilder(head.Lead);
        foreach (var method in methods)
            body.Append(Method(method, Fold.Inner with { Kind = MethodKind }));
        return Group(head, tests, body.ToString());
    }

    private string RequirementFigure(string thingId)
    {
        var map = diagrams.ClassMaps.FirstOrDefault(m => m.Id == thingId);
        return map is null
            ? string.Empty
            : $"<details class=\"rmap\"><summary>Requirement map · {E(map.Map.Header)}</summary>"
              + $"{RunSections.Figure(new FigureSpec("Requirement, method under test, test", map.Map.Mermaid, map.FileName))}</details>";
    }

    private List<(FlowResult Flow, int Index)> FlowSteps(string className) =>
        diagrams.Flows.SelectMany(f => f.Steps.Select((s, i) => (Flow: f, Index: i, Step: s)))
            .Where(p => p.Step.Things.Contains(className, StringComparer.Ordinal))
            .Select(p => (p.Flow, p.Index))
            .ToList();

    private string Feeds(List<(FlowResult Flow, int Index)> steps)
    {
        var next = steps.Where(s => s.Index + 1 < s.Flow.Steps.Count).SelectMany(s => s.Flow.Steps[s.Index + 1].Tests)
            .DistinctBy(t => t.Class, StringComparer.Ordinal).ToList();
        if (next.Count == 0)
            return string.Empty;
        var links = next.Select(t => $"<a href=\"#{E(things.Of(t))}\">{E(MermaidText.ShortClass(t.Class))} &#9656;</a>");
        return $"<span class=\"feeds\">Feeds:&nbsp;{string.Join(" ", links)}</span>";
    }

    private string Method(List<TestResult> tests, Fold fold)
    {
        var first = tests[0];
        var description = first.Description.Length > 0
            ? $"<p class=\"desc\">{E(first.Description)}</p>"
            : "<p class=\"desc empty\">No description yet.</p>";
        var head = fold.Title.Length > 0 ? fold with { Path = [.. fold.Path, first.Method] } : fold with { Title = first.Method };
        var body = new StringBuilder(head.Lead).Append(description).Append($"<div class=\"tablebox\"><table>{TableHead}");
        foreach (var test in tests.OrderBy(t => (int)t.Status).ThenBy(t => t.Name, Ordering))
            body.Append(rows.Row(test));
        return Group(head, tests, body.Append("</table></div>").ToString());
    }

    private static string Group(Fold head, IReadOnlyList<TestResult> tests, string body)
    {
        var id = head.Id is null ? string.Empty : $" id=\"{E(head.Id)}\"";
        var chips = head.Status is null ? string.Empty : Badges.Chips(head.Status);
        var dot = head.Status is null ? Badges.Dot(new Tally(tests)) : string.Empty;
        var titleCss = head.Kind == MethodKind ? "mname" : "ftitle";
        var tag = head.Tag.Length > 0 ? $"<span class=\"kind\">{E(head.Tag)}</span>" : string.Empty;
        var path = head.Path.Count > 0 ? $"<span class=\"gpath\">{E(string.Join('.', head.Path))}</span>" : string.Empty;
        return $"<details class=\"grp {head.Kind}\"{id}><summary><span class=\"tw\"></span><span class=\"gh\">{dot}<span class=\"gname\"><span class=\"{titleCss}\">{E(head.Title)}</span>{tag}</span>"
            + $"<span class=\"tcount\">{Badges.Count(tests.Count)}</span>{head.Meta}{chips}{path}</span></summary><div class=\"gbody\">{body}</div></details>";
    }
}
