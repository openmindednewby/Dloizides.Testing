using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record Fold(string Kind, string? Id, Tally? Status)
{
    public static readonly Fold Inner = new(string.Empty, null, null);

    public IReadOnlyList<string> Crumbs { get; init; } = [];

    public string Meta { get; init; } = string.Empty;

    public string Lead { get; init; } = string.Empty;
}

internal sealed class RunTree(RunDiagrams diagrams)
{
    private const string TableHead =
        "<colgroup><col class=\"c1\"><col class=\"c2\"><col class=\"c3\"><col class=\"c4\"></colgroup>"
        + "<thead><tr><th scope=\"col\">Scenario</th><th scope=\"col\">Expected</th><th scope=\"col\">Result</th><th scope=\"col\" class=\"num\">Time</th></tr></thead>";

    private const string Separator = "<span class=\"sep\"> › </span>";

    private static readonly StringComparer Ordering = StringComparer.OrdinalIgnoreCase;

    private readonly RunRows rows = new();
    private readonly StringBuilder nav = new();
    private int areaCounter;

    public IReadOnlyList<(string Id, string Name)> Problems => rows.Problems;

    public int TestCount => rows.Count;

    public string Nav => nav.ToString();

    public static string ThingId(TestResult test) => $"c-{MermaidText.Slug(test.Project)}-{MermaidText.Slug(test.Class)}";

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
        nav.Append($"<li><a href=\"#{id}\">{Badges.Dot(tally)}{E(feature)}<span class=\"n\">{tests.Count}</span></a></li>");
        var classes = tests.GroupBy(t => (t.Project, t.Class))
            .OrderBy(g => g.Min(t => (int)t.Status)).ThenBy(g => g.Key.Class, Ordering)
            .Select(g => g.ToList()).ToList();
        var head = new Fold("area", id, tally) { Crumbs = [E(feature)], Lead = AreaLead(tests) };
        if (classes.Count == 1)
            return Thing(classes[0], head, multiProject);
        var body = new StringBuilder(head.Lead);
        foreach (var thing in classes)
            body.Append(Thing(thing, Fold.Inner, multiProject));
        return Group(head, tests.Count, body.ToString());
    }

    private string AreaLead(List<TestResult> tests)
    {
        var classes = tests.Select(t => t.Class).ToHashSet(StringComparer.Ordinal);
        var parts = diagrams.Flows.SelectMany(f => f.Steps.Where(s => s.Things.Any(classes.Contains))
            .Select(s => $"Part of: <a href=\"#{E(f.Anchor)}\">{E(f.Name)}</a>, step {s.Step} of {f.LastStep}")).ToList();
        if (diagrams.Schema is not null && classes.Overlaps(diagrams.SchemaClasses))
            parts.Add("<a href=\"#schema-h\">Database diagram</a>");
        return string.Concat(parts.Select(p => $"<p class=\"partof\">{p}</p>"));
    }

    private string Thing(List<TestResult> tests, Fold fold, bool multiProject)
    {
        var first = tests[0];
        var thingId = ThingId(first);
        var project = multiProject ? $"<span class=\"proj\">{E(first.Project)}</span>" : string.Empty;
        var anchor = fold.Id is null ? string.Empty : $"<span class=\"anc\" id=\"{E(thingId)}\"></span>";
        var head = fold with
        {
            Kind = fold.Id is null ? "thing" : fold.Kind,
            Id = fold.Id ?? thingId,
            Crumbs = [.. fold.Crumbs, $"{E(MermaidText.ShortClass(first.Class))}<span class=\"type\">{E(Badges.TypeOf(first.Class))}</span>"],
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
            body.Append(Method(method, Fold.Inner with { Kind = "method" }));
        return Group(head, tests.Count, body.ToString());
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

    private static string Feeds(List<(FlowResult Flow, int Index)> steps)
    {
        var next = steps.Where(s => s.Index + 1 < s.Flow.Steps.Count).SelectMany(s => s.Flow.Steps[s.Index + 1].Tests)
            .DistinctBy(t => t.Class, StringComparer.Ordinal).ToList();
        if (next.Count == 0)
            return string.Empty;
        var links = next.Select(t => $"<a href=\"#{E(ThingId(t))}\">{E(MermaidText.ShortClass(t.Class))} &#9656;</a>");
        return $"<span class=\"feeds\">Feeds:&nbsp;{string.Join(" ", links)}</span>";
    }

    private string Method(List<TestResult> tests, Fold fold)
    {
        var first = tests[0];
        var description = first.Description.Length > 0
            ? $"<p class=\"desc\">{E(first.Description)}</p>"
            : "<p class=\"desc empty\">No description yet.</p>";
        var head = fold with { Crumbs = [.. fold.Crumbs, $"<span class=\"mname\">{E(first.Method)}</span>"] };
        var body = new StringBuilder(description).Append(head.Lead).Append($"<div class=\"tablebox\"><table>{TableHead}");
        foreach (var test in tests.OrderBy(t => (int)t.Status).ThenBy(t => t.Name, Ordering))
            body.Append(rows.Row(test));
        return Group(head, tests.Count, body.Append("</table></div>").ToString());
    }

    private static string Group(Fold head, int scenarios, string body)
    {
        var id = head.Id is null ? string.Empty : $" id=\"{E(head.Id)}\"";
        var chips = head.Status is null ? string.Empty : Badges.Chips(head.Status);
        return $"<details class=\"grp {head.Kind}\"{id}><summary><span class=\"tw\"></span><span class=\"gh\"><span class=\"gname\">{string.Join(Separator, head.Crumbs)}</span>"
            + $"<span class=\"tcount\">{Badges.Count(scenarios)}</span>{head.Meta}{chips}</span></summary><div class=\"gbody\">{body}</div></details>";
    }
}
