using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record FigureSpec(string Caption, string Mermaid, string FileName)
{
    public string Id { get; init; } = string.Empty;

    public string Links { get; init; } = string.Empty;
}

internal static class RunSections
{
    public const string Toolbar =
        "<div class=\"tools\" role=\"search\"><label class=\"sr\" for=\"q\">Search tests</label>"
        + "<input id=\"q\" type=\"search\" placeholder=\"Search scenario, method, class or reason\" autocomplete=\"off\">"
        + "<button type=\"button\" id=\"openAll\">Expand all</button><button type=\"button\" id=\"closeAll\">Collapse all</button>"
        + "<output id=\"qn\" for=\"q\" aria-live=\"polite\"></output><p id=\"qz\" class=\"none\" hidden></p></div>"
        + "<p class=\"legend\"><span><span class=\"dot red\"></span> failed or passed early</span>"
        + "<span><span class=\"dot amber\"></span> skipped, needs a look</span><span><span class=\"sw xfail\"></span> expected red</span></p>";

    private static string E(string text) => Html.Encode(text);

    public static string Diagrams(RunDiagrams diagrams, Func<TestResult, string> thingId)
    {
        var builder = new StringBuilder();
        if (diagrams.Requirements is { } map)
            builder.Append(Requirements(map, thingId));
        if (diagrams.Flows.Count > 0)
        {
            builder.Append("<section class=\"diagrams\" aria-labelledby=\"flows-h\"><h2 id=\"flows-h\">Flows</h2>");
            foreach (var flow in diagrams.Flows)
                builder.Append(Figure(new FigureSpec(flow.Name, flow.Mermaid, flow.FileName) { Id = flow.Anchor, Links = StepLinks(flow, thingId) }));
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
        var links = flow.Steps.SelectMany(s => s.Tests.DistinctBy(MermaidText.MethodKey, StringComparer.Ordinal)
            .Select(t => $"<li><a href=\"#{E(thingId(t))}\">Step {s.Step}: {E(MermaidText.MethodKey(t))}</a></li>"));
        return $"<ol class=\"fsteps\">{string.Concat(links)}</ol>";
    }

    public static string Figure(FigureSpec spec)
    {
        var id = spec.Id.Length > 0 ? $" id=\"{E(spec.Id)}\"" : string.Empty;
        return $"<figure class=\"diagram\"{id}><div class=\"scroll\"><div class=\"mermaid\">{E(spec.Mermaid)}</div></div>{spec.Links}<figcaption>{E(spec.Caption)} · "
            + $"<a href=\"{E(RunDiagrams.PageOf(spec.FileName))}\">Open full size</a> · <a href=\"{E(spec.FileName)}\">{E(spec.FileName)}</a></figcaption></figure>";
    }
}
