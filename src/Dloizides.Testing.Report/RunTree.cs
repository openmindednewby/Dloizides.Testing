using System.Text;

namespace Dloizides.Testing.Report;

internal sealed class RunTree(IReadOnlyList<FlowResult> flows)
{
    private const string TableHead =
        "<colgroup><col class=\"c1\"><col class=\"c2\"><col class=\"c3\"><col class=\"c4\"></colgroup>"
        + "<thead><tr><th scope=\"col\">Scenario</th><th scope=\"col\">Expected</th><th scope=\"col\">Result</th><th scope=\"col\" class=\"num\">Time</th></tr></thead>";

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
        var builder = new StringBuilder($"<details class=\"area\" id=\"{id}\"><summary><span class=\"tw\"></span>{Badges.Dot(tally)}")
            .Append($"<span class=\"aname\">{E(feature)}</span>{Badges.Chips(tally)}</summary><div class=\"abody\">");
        var things = tests.GroupBy(t => (t.Project, t.Class))
            .OrderBy(g => g.Min(t => (int)t.Status)).ThenBy(g => g.Key.Class, Ordering);
        foreach (var thing in things)
            builder.Append(Thing(thing.ToList(), multiProject));
        return builder.Append("</div></details>").ToString();
    }

    private string Thing(List<TestResult> tests, bool multiProject)
    {
        var first = tests[0];
        var tally = new Tally(tests);
        var project = multiProject ? $"<span class=\"proj\">{E(first.Project)}</span>" : string.Empty;
        var steps = FlowSteps(first.Class);
        var builder = new StringBuilder($"<details class=\"thing\" id=\"{E(ThingId(first))}\"><summary><span class=\"tw\"></span>{Badges.Dot(tally)}")
            .Append($"<span class=\"tname\">{E(MermaidText.ShortClass(first.Class))}</span><span class=\"type\">{E(Badges.TypeOf(first.Class))}</span>")
            .Append($"{project}<span class=\"tcount\">{Badges.Count(tests.Count)}</span>{Feeds(steps)}</summary>");
        foreach (var (flow, index) in steps)
            builder.Append(FlowLine(flow, index));
        builder.Append("<div class=\"tbody\">");
        var methods = tests.GroupBy(t => t.Method, StringComparer.Ordinal)
            .OrderBy(g => g.Min(t => (int)t.Status)).ThenBy(g => g.Key, Ordering);
        foreach (var method in methods)
            builder.Append(Method(method.ToList()));
        return builder.Append("</div></details>").ToString();
    }

    private List<(FlowResult Flow, int Index)> FlowSteps(string className) =>
        flows.SelectMany(f => f.Steps.Select((s, i) => (Flow: f, Index: i, Step: s)))
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

    private static string FlowLine(FlowResult flow, int index)
    {
        var step = flow.Steps[index].Step;
        var marks = string.Concat(Enumerable.Range(1, flow.LastStep).Select(i => $"<i class=\"{(i <= step ? "on" : string.Empty)}\"></i>"));
        var path = string.Join(" &rarr; ", flow.Steps.Select(s => E(string.Join(", ", s.Things.Select(MermaidText.ShortClass)))));
        return $"<div class=\"flow\"><b>{E(flow.Name)}: step {step} of {flow.LastStep}</b><span class=\"steps\" aria-hidden=\"true\">{marks}</span><span>{path}</span></div>";
    }

    private string Method(List<TestResult> tests)
    {
        var first = tests[0];
        var description = first.Description.Length > 0
            ? $"<p class=\"desc\">{E(first.Description)}</p>"
            : "<p class=\"desc empty\">No description yet.</p>";
        var builder = new StringBuilder($"<details class=\"method\"><summary><span class=\"tw\"></span>{Badges.Dot(new Tally(tests))}")
            .Append($"<span class=\"mname\">{E(first.Method)}</span><span class=\"tcount\">{Badges.Count(tests.Count)}</span></summary>")
            .Append($"{description}<div class=\"tablebox\"><table>{TableHead}");
        foreach (var test in tests.OrderBy(t => (int)t.Status).ThenBy(t => t.Name, Ordering))
            builder.Append(rows.Row(test));
        return builder.Append("</table></div></details>").ToString();
    }
}
