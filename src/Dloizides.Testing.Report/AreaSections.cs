namespace Dloizides.Testing.Report;

internal static class AreaSections
{
    private const string NotSet = "not set";

    private static string E(string text) => Html.Encode(text);

    public static string Why(ResultsFeature? feature) =>
        feature is null
            ? string.Empty
            : $"<div class=\"whycard\"><dl>{Row("Why", feature.Why)}{Row("Context", feature.Context)}{Row("Owner", feature.Owner)}</dl></div>";

    public static string UseCases(AreaDiagram? diagram) =>
        diagram is null ? string.Empty : $"<div class=\"usecases\">{Figure(diagram)}</div>";

    public static string Flows(IEnumerable<FlowResult> flows) =>
        string.Concat(flows.Select(flow => $"<div class=\"areaflow\"><a href=\"#{E(flow.Anchor)}\">Flow: {E(flow.Name)}</a><ol class=\"fsteps\">"
            + string.Concat(flow.Steps.Select(s => $"<li>Step {s.Step}: {E(MermaidText.ShortClass(s.Class))}</li>")) + "</ol></div>"));

    public static string Sequence(AreaDiagram? diagram) =>
        diagram is null ? string.Empty : $"<details class=\"seq\"><summary>Sequence · {E(diagram.Caption)}</summary>{Figure(diagram)}</details>";

    private static string Figure(AreaDiagram diagram) => RunSections.Figure(new FigureSpec(diagram.Caption, diagram.Mermaid, diagram.FileName));

    private static string Row(string label, string value) =>
        value.Trim().Length > 0 ? $"<dt>{label}</dt><dd>{E(value)}</dd>" : $"<dt>{label}</dt><dd class=\"unset\">{NotSet}</dd>";
}
