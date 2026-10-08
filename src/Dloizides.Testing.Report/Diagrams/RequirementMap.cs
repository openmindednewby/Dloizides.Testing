using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record RequirementCard(int Index, string Id, string Title, string Class, IReadOnlyList<TestResult> Tests, bool Declared);

internal sealed record RequirementMapResult(string Mermaid, IReadOnlyList<RequirementCard> Cards)
{
    public const string FileName = "requirements.mmd";

    public int Undeclared => Cards.Count(c => !c.Declared);

    public string Header
    {
        get
        {
            var declared = Cards.Count(c => c.Declared);
            var uncovered = Cards.Count(c => c.Declared && c.Tests.Count == 0);
            var parts = new List<string> { $"{declared} requirement{(declared == 1 ? string.Empty : "s")}" };
            if (Undeclared > 0)
                parts.Add($"{Undeclared} undeclared");
            if (uncovered > 0)
                parts.Add($"{uncovered} without tests");
            return string.Join(" · ", parts);
        }
    }
}

internal sealed record NodeRegistry(Dictionary<string, string> Ids, StringBuilder Builder);

internal static class RequirementMap
{
    private const string Indent = "    ";

    public static RequirementMapResult Render(IReadOnlyList<ResultsRequirement> requirements, IReadOnlyList<TestResult> tests)
    {
        var declaredIds = requirements.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        var undeclared = tests.SelectMany(t => t.Covers).Where(id => !declaredIds.Contains(id)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        var entries = requirements.DistinctBy(r => r.Id, StringComparer.Ordinal).Select(r => (r.Id, r.Title, Declared: true))
            .Concat(undeclared.Select(id => (Id: id, Title: string.Empty, Declared: false)))
            .ToList();
        var cards = entries.Select((e, i) => Card(i, e, tests)).ToList();
        return new RequirementMapResult(Draw(cards), cards);
    }

    private static RequirementCard Card(int index, (string Id, string Title, bool Declared) entry, IReadOnlyList<TestResult> tests)
    {
        var covering = tests.Where(t => t.Covers.Contains(entry.Id, StringComparer.Ordinal)).ToList();
        return new RequirementCard(index, entry.Id, entry.Title, DiagramClass.ForWorst(covering, DiagramClass.Empty), covering, entry.Declared);
    }

    private static string Draw(IReadOnlyList<RequirementCard> cards)
    {
        var nodes = new StringBuilder("flowchart LR\n");
        Group(nodes, "declared", "Requirements", cards.Where(c => c.Declared).ToList());
        Group(nodes, "undeclared", $"Undeclared ids ({cards.Count(c => !c.Declared)})", cards.Where(c => !c.Declared).ToList());
        var edges = new StringBuilder();
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var registry = new NodeRegistry(ids, nodes);
        foreach (var card in cards)
        {
            foreach (var method in card.Tests.GroupBy(MermaidText.MethodKey))
            {
                var methodId = Node(registry, "m:" + method.Key, "m", method.Key, DiagramClass.ForWorst(method, DiagramClass.Empty));
                edges.Append($"{Indent}r{card.Index} --> {methodId}\n");
                foreach (var test in method)
                {
                    var label = test.Scenario.Length > 0 ? $"{test.Scenario}: {test.Expected}" : test.Expected;
                    var testId = Node(registry, "t:" + test.Name, "t", label, DiagramClass.For(test.Status));
                    edges.Append($"{Indent}{methodId} --> {testId}\n");
                }
            }
        }

        return nodes.Append(edges).Append(DiagramClass.ClassDefs(Indent)).ToString();
    }

    private static void Group(StringBuilder builder, string id, string title, IReadOnlyList<RequirementCard> members)
    {
        if (members.Count == 0)
            return;
        builder.Append($"{Indent}subgraph {id}[\"{MermaidText.Label(title)}\"]\n");
        foreach (var card in members)
        {
            var text = card.Title.Length > 0 ? $"{card.Id}: {card.Title}" : card.Id;
            builder.Append($"{Indent}{Indent}r{card.Index}[\"{MermaidText.Label(text)}\"]:::{card.Class}\n");
        }

        builder.Append($"{Indent}end\n");
    }

    private static string Node(NodeRegistry registry, string key, string prefix, string label, string cssClass)
    {
        var (ids, builder) = (registry.Ids, registry.Builder);
        if (ids.TryGetValue(key, out var existing))
            return existing;
        var id = $"{prefix}{ids.Count}";
        ids[key] = id;
        builder.Append($"{Indent}{id}[\"{MermaidText.Label(label)}\"]:::{cssClass}\n");
        return id;
    }
}
