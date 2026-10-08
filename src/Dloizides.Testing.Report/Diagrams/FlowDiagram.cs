using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record FlowStep(int Step, string Class, IReadOnlyList<TestResult> Tests)
{
    public IReadOnlyList<string> Things => Tests.Select(t => t.Class).Distinct(StringComparer.Ordinal).ToList();

    public string MermaidLabel => $"Step {Step}: {string.Join("<br/>", Tests.Select(MermaidText.MethodKey).Distinct(StringComparer.Ordinal).Select(MermaidText.Label))}";
}

internal sealed record FlowResult(string Name, IReadOnlyList<FlowStep> Steps, string Mermaid)
{
    public string FileName => $"flow-{MermaidText.Slug(Name)}.mmd";

    public int LastStep => Steps.Count == 0 ? 0 : Steps[^1].Step;
}

internal static class FlowDiagram
{
    private const string Indent = "    ";

    public static IReadOnlyList<FlowResult> Render(IReadOnlyList<TestResult> tests) =>
        tests
            .SelectMany(t => t.Flows.Select(f => (Flow: f, Test: t)))
            .GroupBy(p => p.Flow.Name, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => Flow(g.Key, g.GroupBy(p => p.Flow.Step).OrderBy(s => s.Key)
                .Select(s => Step(s.Key, s.Select(p => p.Test).Distinct().ToList())).ToList()))
            .ToList();

    private static FlowStep Step(int step, IReadOnlyList<TestResult> tests) =>
        new(step, DiagramClass.ForWorst(tests, DiagramClass.Empty), tests);

    private static FlowResult Flow(string name, IReadOnlyList<FlowStep> steps)
    {
        var builder = new StringBuilder("flowchart LR\n");
        builder.Append($"{Indent}subgraph flow[\"{MermaidText.Label(name)}\"]\n");
        foreach (var step in steps)
            builder.Append($"{Indent}{Indent}s{step.Step}[\"{step.MermaidLabel}\"]:::{step.Class}\n");
        builder.Append($"{Indent}end\n");
        for (var i = 1; i < steps.Count; i++)
            builder.Append($"{Indent}s{steps[i - 1].Step} --> s{steps[i].Step}\n");
        return new FlowResult(name, steps, builder.Append(DiagramClass.ClassDefs(Indent)).ToString());
    }
}
