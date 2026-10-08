using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record AreaDiagram(string Area, string FileName, string Mermaid, string Caption);

internal static class UseCaseDiagram
{
    private const string Indent = "    ";
    private const string ActorStyle = "fill:#e8edf2,stroke:#556372,color:#17212b";
    private const string NoActor = "Actor not set";
    private const string Caption = "Use cases, coloured by the tests that prove them";

    public static IReadOnlyList<AreaDiagram> Render(IReadOnlyList<TestResult> tests) =>
        tests.Where(t => t.UseCases.Count > 0)
            .GroupBy(t => t.Feature, StringComparer.Ordinal)
            .Select(g => new AreaDiagram(g.Key, $"usecases-{MermaidText.Slug(g.Key)}.mmd", Draw(g.ToList()), Caption))
            .ToList();

    private static string Draw(List<TestResult> tests)
    {
        var cases = tests.SelectMany(t => t.UseCases.Select(u => (Case: u, Test: t)))
            .GroupBy(p => p.Case)
            .Select(g => (UseCase: g.Key, Class: DiagramClass.ForWorst(g.Select(p => p.Test), DiagramClass.Empty)))
            .ToList();
        var actors = cases.Select(c => Actor(c.UseCase)).Distinct(StringComparer.Ordinal).ToList();
        var builder = new StringBuilder("flowchart LR\n");
        for (var i = 0; i < actors.Count; i++)
            builder.Append($"{Indent}a{i + 1}[\"{MermaidText.Label(actors[i])}\"]:::actor\n");
        for (var i = 0; i < cases.Count; i++)
            builder.Append($"{Indent}u{i + 1}([\"{MermaidText.Label(cases[i].UseCase.Text)}\"]):::{cases[i].Class}\n");
        for (var i = 0; i < cases.Count; i++)
            builder.Append($"{Indent}a{actors.IndexOf(Actor(cases[i].UseCase)) + 1} --- u{i + 1}\n");
        return builder.Append($"{Indent}classDef actor {ActorStyle}\n").Append(DiagramClass.ClassDefs(Indent)).ToString();
    }

    private static string Actor(UseCaseEntry useCase) => useCase.Actor.Trim().Length > 0 ? useCase.Actor : NoActor;
}

internal static class SequenceDiagram
{
    private const string Indent = "    ";
    private const string UnnamedCall = "call";

    public static IReadOnlyList<AreaDiagram> Render(IReadOnlyList<TestResult> tests) =>
        tests.Where(t => t.Calls.Count > 0)
            .GroupBy(t => t.Feature, StringComparer.Ordinal)
            .Select(g => g.OrderBy(t => (int)t.Status).ThenBy(t => t.Name, StringComparer.Ordinal).First())
            .Select(t => new AreaDiagram(t.Feature, $"sequence-{MermaidText.Slug(t.Feature)}.mmd", Draw(t.Calls), $"recorded calls of {t.Name}"))
            .ToList();

    private static string Draw(IReadOnlyList<ResultsCall> calls)
    {
        var ordered = calls.OrderBy(c => c.Seq).ToList();
        var parties = ordered.SelectMany(c => new[] { c.From, c.To }).Distinct(StringComparer.Ordinal).ToList();
        var builder = new StringBuilder("sequenceDiagram\n");
        for (var i = 0; i < parties.Count; i++)
            builder.Append($"{Indent}participant p{i + 1} as {Text(parties[i])}\n");
        foreach (var call in ordered)
        {
            var from = $"p{parties.IndexOf(call.From) + 1}";
            var to = $"p{parties.IndexOf(call.To) + 1}";
            var message = string.Join(" ", new[] { call.Method, call.Path }.Where(p => !string.IsNullOrEmpty(p)));
            builder.Append($"{Indent}{from}->>{to}: {Text(message.Length > 0 ? message : UnnamedCall)}\n");
            if (call.Status is { } status)
                builder.Append($"{Indent}{to}-->>{from}: {status}\n");
        }

        return builder.ToString();
    }

    private static string Text(string text) => MermaidText.Label(text).Replace(";", "#59;", StringComparison.Ordinal);
}
