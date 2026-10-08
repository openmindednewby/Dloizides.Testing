using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record RunDiagrams(RequirementMapResult? Requirements, IReadOnlyList<FlowResult> Flows, SchemaResult? Schema)
{
    public const string MermaidScript =
        "<script src=\"https://cdnjs.cloudflare.com/ajax/libs/mermaid/11.6.0/mermaid.min.js\" integrity=\"sha512-3Ix7UjkWptQ1zS6VvZzzy4QPkYfhcr5fFOmxbsvgT/83J563DkKHEVHV0jmFNtSyfRU9SLfLsGTly/QpkzegHQ==\" crossorigin=\"anonymous\" referrerpolicy=\"no-referrer\"></script>"
        + "<script>var dark=matchMedia(\"(prefers-color-scheme: dark)\").matches;mermaid.initialize({startOnLoad:true,securityLevel:\"strict\","
        + "theme:dark?\"dark\":\"neutral\",flowchart:{useMaxWidth:false,wrappingWidth:400},er:{useMaxWidth:false}});</script>";

    private const string MigrationTargetSet = "MigrationTarget";

    public static readonly RunDiagrams None = new(null, [], null);

    public IReadOnlyList<ClassMap> ClassMaps { get; init; } = [];

    public IReadOnlySet<string> SchemaClasses { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public bool Any => Requirements is not null || Flows.Count > 0 || Schema is not null;

    public IEnumerable<(string FileName, string Mermaid)> Files
    {
        get
        {
            foreach (var map in ClassMaps)
                yield return (map.FileName, map.Map.Mermaid);
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
        var schemaTests = run.Sets.Where(IsSchemaTarget).SelectMany(s => s.Tests).ToList();
        var schema = snapshotSource is null ? null : SchemaDiagram.RenderSnapshot(snapshotSource, schemaTests);
        return new RunDiagrams(requirements, FlowDiagram.Render(tests), schema)
        {
            ClassMaps = hasRequirements ? ClassMapsOf(run.Requirements, tests, ThingIds.For(tests)) : [],
            SchemaClasses = schema is null ? new HashSet<string>(StringComparer.Ordinal) : schemaTests.Select(t => t.Class).ToHashSet(StringComparer.Ordinal),
        };
    }

    private static List<ClassMap> ClassMapsOf(IReadOnlyList<ResultsRequirement> requirements, IReadOnlyList<TestResult> tests, ThingIds things) =>
        tests.Where(t => t.Covers.Count > 0)
            .GroupBy(things.Of, StringComparer.Ordinal)
            .Select(g => new ClassMap(g.Key, RequirementMap.Render(requirements.Where(r => g.Any(t => t.Covers.Contains(r.Id, StringComparer.Ordinal))).ToList(), g.ToList())))
            .ToList();

    private static bool IsSchemaTarget(TestSet set) =>
        set.ExpectRed || string.Equals(set.Name, MigrationTargetSet, StringComparison.OrdinalIgnoreCase);

    public void WriteFiles(string folder, Encoding encoding)
    {
        foreach (var (fileName, mermaid) in Files)
        {
            File.WriteAllText(Path.Combine(folder, fileName), mermaid, encoding);
            File.WriteAllText(Path.Combine(folder, PageOf(fileName)), FullSizePage(fileName, mermaid), encoding);
        }
    }

    public static string PageOf(string fileName) => Path.ChangeExtension(fileName, ".html");

    private static string FullSizePage(string fileName, string mermaid) =>
        "<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">"
        + $"<title>{Html.Encode(fileName)}</title><style>:root{{color-scheme:light dark}}body{{margin:0;padding:16px;font:15px/1.5 system-ui,sans-serif}}</style></head>"
        + $"<body><div class=\"mermaid\">{Html.Encode(mermaid)}</div>{MermaidScript}</body></html>";
}
