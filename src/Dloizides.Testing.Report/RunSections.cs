using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record FigureSpec(string Caption, string Mermaid, string FileName)
{
    public string Id { get; init; } = string.Empty;

    public string Links { get; init; } = string.Empty;
}

internal static class RunSections
{
    private const string Legend =
        "<p class=\"legend\"><span><span class=\"dot red\"></span> failed or passed early</span>"
        + "<span><span class=\"dot amber\"></span> skipped, needs a look</span><span><span class=\"sw xfail\"></span> expected red</span></p>";

    private const string NoTask = "-";

    public static string Toolbar(IReadOnlyList<TestResult> tests, IReadOnlyList<ResultsFeature> features, TaskLinks tasks)
    {
        var outcomes = tests.Select(t => t.Status).Distinct().Order().Select(s => Option(StatusText.Css(s), StatusText.Label(s)));
        var areas = tests.GroupBy(t => t.Feature, StringComparer.Ordinal).OrderBy(g => g.Min(t => (int)t.Status)).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key).Select(f => Option(f, FeatureTitle.Find(features, f)?.Name ?? FeatureTitle.Words(f)));
        var ids = TaskIds(tests, tasks);
        var builder = new StringBuilder("<div class=\"tools\" role=\"search\"><label class=\"sr\" for=\"q\">Search tests</label>")
            .Append("<input id=\"q\" type=\"search\" placeholder=\"Search tests or tasks\" autocomplete=\"off\">")
            .Append(Select("fo", "Outcome", "Every outcome", outcomes))
            .Append(Select("ff", "Area", "Every area", areas));
        if (ids.Count > 0)
        {
            builder.Append(TaskCombo(ids, tests.Any(t => tasks.Of(t).Count == 0)))
                .Append("<label class=\"sr\" for=\"gb\">Group by</label><select id=\"gb\"><option value=\"area\">Group by area</option><option value=\"task\">Group by task</option></select>");
        }

        return builder.Append("<button type=\"button\" id=\"openAll\">Expand all</button><button type=\"button\" id=\"closeAll\">Collapse all</button>")
            .Append("<output id=\"qn\" for=\"q\" aria-live=\"polite\"></output><p id=\"qz\" class=\"none\" hidden></p></div>")
            .Append(Legend).ToString();
    }

    public static IReadOnlyList<string> TaskIds(IEnumerable<TestResult> tests, TaskLinks tasks) =>
        tests.SelectMany(tasks.Of).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

    public static string ByTask(IReadOnlyList<TestResult> tests, TaskLinks tasks, Func<TestResult, string> idOf)
    {
        var groups = tests.SelectMany(t => tasks.Of(t).Select(k => (Task: k, Test: t)))
            .GroupBy(p => p.Task, p => p.Test, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
        if (groups.Count == 0)
            return string.Empty;
        var builder = new StringBuilder("<section id=\"bytask\" class=\"bytask\" aria-labelledby=\"bytask-h\" hidden><h2 id=\"bytask-h\">Tests by task</h2>");
        foreach (var group in groups)
        {
            var list = group.ToList();
            builder.Append($"<details class=\"task\" data-k=\"{E(group.Key)}\"><summary>{tasks.Link(group.Key, "tid")}<span class=\"n\">{list.Count} tests</span>{Badges.Chips(new Tally(list))}</summary><ul>")
                .Append(string.Concat(list.Select(t => $"<li><a href=\"#{idOf(t)}\">{E(FeatureTitle.Words(t.Feature))} · {E(t.Class)} · {E(t.Scenario.Length > 0 ? t.Scenario : t.Method)}</a></li>")))
                .Append("</ul></details>");
        }

        var untagged = tests.Count(t => tasks.Of(t).Count == 0);
        if (untagged > 0)
            builder.Append($"<p class=\"when\">{untagged} tests name no task.</p>");
        return builder.Append("</section>").ToString();
    }

    private static string TaskCombo(IReadOnlyList<string> ids, bool hasUntagged)
    {
        var values = new List<(string Value, string Label)> { (string.Empty, "Every task") };
        values.AddRange(ids.Select(i => (i, i)));
        if (hasUntagged)
            values.Add((NoTask, "No task"));
        var options = values.Select((v, i) => $"<li role=\"option\" id=\"fko-{i}\" data-v=\"{E(v.Value)}\" aria-selected=\"false\">{E(v.Label)}</li>");
        return "<div class=\"combo\"><label class=\"sr\" for=\"fkq\">Task, type to search</label>"
            + "<input id=\"fkq\" type=\"text\" role=\"combobox\" aria-autocomplete=\"list\" aria-expanded=\"false\" aria-controls=\"fkl\" placeholder=\"Every task\" autocomplete=\"off\" spellcheck=\"false\">"
            + $"<input id=\"fk\" type=\"hidden\" value=\"\"><ul id=\"fkl\" role=\"listbox\" aria-label=\"Tasks\" hidden>{string.Concat(options)}</ul></div>";
    }

    private static string Option(string value, string label) => $"<option value=\"{E(value)}\">{E(label)}</option>";

    private static string Select(string id, string label, string all, IEnumerable<string> options) =>
        $"<label class=\"sr\" for=\"{id}\">{label}</label><select id=\"{id}\"><option value=\"\">{all}</option>{string.Concat(options)}</select>";

    private static string E(string text) => Html.Encode(text);

    public static string Diagrams(RunDiagrams diagrams, Func<TestResult, string> thingId)
    {
        var builder = new StringBuilder();
        if (diagrams.Requirements is { } map)
            builder.Append(Requirements(map, thingId));
        if (diagrams.Flows.Count > 0)
        {
            builder.Append("<section class=\"flows\" aria-labelledby=\"flows-h\"><h2 id=\"flows-h\">Flows</h2>");
            foreach (var flow in diagrams.Flows)
                builder.Append($"<div class=\"flow\" id=\"{E(flow.Anchor)}\"><h3>{E(flow.Name)}</h3>{StepLinks(flow, thingId)}</div>");
            builder.Append("</section>");
        }

        if (diagrams.Schema is { } schema)
            builder.Append("<section class=\"diagrams\" aria-labelledby=\"schema-h\"><h2 id=\"schema-h\">Database</h2>")
                .Append(Figure(new FigureSpec("Tables from the EF model snapshot, coloured by the tests named after them", schema.Mermaid, SchemaResult.FileName)))
                .Append("</section>");
        return builder.ToString();
    }

    private static string Requirements(RequirementMapResult map, Func<TestResult, string> thingId)
    {
        var builder = new StringBuilder("<section class=\"req\" aria-labelledby=\"req-h\"><header><h2 id=\"req-h\">Requirement map</h2>")
            .Append($"<span class=\"rcount\">{E(map.Header)}</span></header><ol>");
        foreach (var card in map.Cards)
        {
            var title = card.Declared ? card.Title : "Not declared by any [Requirement]";
            var link = card.Tests.Count == 0
                ? "<span class=\"none-yet\">No test covers it yet</span>"
                : $"<a href=\"#{E(thingId(card.Tests[0]))}\">{E(new Tally(card.Tests).Text())} · {E(string.Join(", ", card.Tests.Select(t => MermaidText.ShortClass(t.Class)).Distinct(StringComparer.Ordinal)))}</a>";
            builder.Append($"<li class=\"tone-{card.Class}\"><span class=\"rid\">{E(card.Id)}</span><span class=\"rt\">{E(title)}</span>{link}</li>");
        }

        return builder.Append("</ol></section>").ToString();
    }

    private static string StepLinks(FlowResult flow, Func<TestResult, string> thingId)
    {
        var steps = flow.Steps.GroupBy(s => s.Step).OrderBy(g => g.Key).Select(g =>
        {
            var links = g.SelectMany(s => s.Tests).DistinctBy(MermaidText.MethodKey, StringComparer.Ordinal)
                .Select(t => $"<a href=\"#{E(thingId(t))}\">{E(MermaidText.MethodKey(t))}</a>");
            return $"<li><span class=\"sn\">Step {g.Key}</span>{string.Concat(links)}</li>";
        });
        return $"<ol class=\"fsteps\">{string.Concat(steps)}</ol>";
    }

    public static string Figure(FigureSpec spec)
    {
        var id = spec.Id.Length > 0 ? $" id=\"{E(spec.Id)}\"" : string.Empty;
        return $"<figure class=\"diagram\"{id}><div class=\"scroll\"><div class=\"mermaid\">{E(spec.Mermaid)}</div></div>{spec.Links}<figcaption>{E(spec.Caption)} · "
            + $"<a href=\"{E(RunDiagrams.PageOf(spec.FileName))}\">Open full size</a> · <a href=\"{E(spec.FileName)}\">{E(spec.FileName)}</a></figcaption></figure>";
    }
}
