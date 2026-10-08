using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record RunDiagrams(RequirementMapResult? Requirements, IReadOnlyList<FlowResult> Flows, SchemaResult? Schema)
{
    public const string MermaidScript =
        "<script src=\"https://cdnjs.cloudflare.com/ajax/libs/mermaid/11.6.0/mermaid.min.js\" crossorigin=\"anonymous\" referrerpolicy=\"no-referrer\"></script>"
        + "<script>mermaid.initialize({startOnLoad:true,securityLevel:\"strict\",theme:\"neutral\"});</script>";

    public static readonly RunDiagrams None = new(null, [], null);

    public bool Any => Requirements is not null || Flows.Count > 0 || Schema is not null;

    public IEnumerable<(string FileName, string Mermaid)> Files
    {
        get
        {
            if (Requirements is not null)
                yield return (RequirementMapResult.FileName, Requirements.Mermaid);
            foreach (var flow in Flows)
                yield return (flow.FileName, flow.Mermaid);
            if (Schema is not null)
                yield return (SchemaResult.FileName, Schema.Mermaid);
        }
    }

    public static RunDiagrams Build(TestRun run, string? snapshotSource)
    {
        var tests = run.Sets.SelectMany(s => s.Tests).ToList();
        var hasRequirements = run.Requirements.Count > 0 || tests.Any(t => t.Covers.Count > 0);
        var requirements = hasRequirements ? RequirementMap.Render(run.Requirements, tests) : null;
        var schema = snapshotSource is null ? null : SchemaDiagram.RenderSnapshot(snapshotSource, tests);
        return new RunDiagrams(requirements, FlowDiagram.Render(tests), schema);
    }

    public void WriteFiles(string folder, Encoding encoding)
    {
        foreach (var (fileName, mermaid) in Files)
            File.WriteAllText(Path.Combine(folder, fileName), mermaid, encoding);
    }
}
