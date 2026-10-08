using System.Text;
using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal sealed record SchemaRelation(string Principal, string Dependent, string ForeignKey);

internal sealed record SchemaModel(IReadOnlyList<string> Tables, IReadOnlyList<SchemaRelation> Relations);

internal sealed record SchemaResult(string Mermaid, IReadOnlyDictionary<string, string> TableClasses)
{
    public const string FileName = "schema.mmd";
}

internal static partial class SchemaDiagram
{
    private const string Indent = "    ";
    private const string TargetSuffix = "Target";

    [GeneratedRegex(@"\bEntity\(\s*""(?<name>[^""]+)""\s*,\s*\w+\s*=>")]
    private static partial Regex EntityBlock();

    [GeneratedRegex(@"\bToTable\(\s*""(?<table>[^""]+)""")]
    private static partial Regex ToTable();

    [GeneratedRegex(@"\bHasOne\(\s*""(?<target>[^""]+)""[^;]*?\.HasForeignKey\(\s*""(?<fk>[^""]+)""", RegexOptions.Singleline)]
    private static partial Regex HasOne();

    [GeneratedRegex("[_-]+")]
    private static partial Regex Separators();

    public static SchemaModel ReadSnapshot(string source)
    {
        var blocks = EntityBlock().Matches(source).ToList();
        var tables = new Dictionary<string, string>(StringComparer.Ordinal);
        var links = new List<(string Dependent, string Principal, string ForeignKey)>();
        for (var i = 0; i < blocks.Count; i++)
        {
            var start = blocks[i].Index + blocks[i].Length;
            var end = i + 1 < blocks.Count ? blocks[i + 1].Index : source.Length;
            var body = source[start..end];
            var entity = blocks[i].Groups["name"].Value;
            if (ToTable().Match(body) is { Success: true } table && !tables.ContainsKey(entity))
                tables[entity] = table.Groups["table"].Value;
            links.AddRange(HasOne().Matches(body).Select(m => (entity, m.Groups["target"].Value, m.Groups["fk"].Value)));
        }

        string TableOf(string entity) => tables.GetValueOrDefault(entity) ?? entity[(entity.LastIndexOf('.') + 1)..];
        var names = blocks.Select(b => TableOf(b.Groups["name"].Value)).Distinct(StringComparer.Ordinal).ToList();
        var relations = links.Select(l => new SchemaRelation(TableOf(l.Principal), TableOf(l.Dependent), l.ForeignKey)).Distinct().ToList();
        return new SchemaModel(names, relations);
    }

    public static SchemaResult RenderSnapshot(string source, IReadOnlyList<TestResult> tests) => Render(ReadSnapshot(source), tests);

    public static SchemaResult Render(SchemaModel model, IReadOnlyList<TestResult> tests)
    {
        var entities = EntityIds(model.Tables);
        var classes = model.Tables.ToDictionary(
            t => t,
            t => DiagramClass.ForWorst(tests.Where(test => Covers(test, t)), DiagramClass.Plain),
            StringComparer.Ordinal);
        var builder = new StringBuilder("erDiagram\n");
        foreach (var table in model.Tables)
            builder.Append($"{Indent}{entities[table]} {{\n{Indent}{Indent}string table \"{MermaidText.Label(table)}\"\n{Indent}}}\n");
        foreach (var relation in model.Relations.Where(r => entities.ContainsKey(r.Principal) && entities.ContainsKey(r.Dependent)))
            builder.Append($"{Indent}{entities[relation.Principal]} ||--o{{ {entities[relation.Dependent]} : \"{MermaidText.Label(relation.ForeignKey)}\"\n");
        builder.Append(DiagramClass.ClassDefs(Indent));
        foreach (var table in model.Tables)
            builder.Append($"{Indent}class {entities[table]} {classes[table]}\n");
        return new SchemaResult(builder.ToString(), classes);
    }

    private static Dictionary<string, string> EntityIds(IReadOnlyList<string> tables)
    {
        var ids = new Dictionary<string, string>(StringComparer.Ordinal);
        var taken = new HashSet<string>(StringComparer.Ordinal);
        foreach (var table in tables)
        {
            var id = MermaidText.Entity(table);
            var unique = id;
            for (var n = 2; !taken.Add(unique); n++)
                unique = $"{id}_{n}";
            ids[table] = unique;
        }

        return ids;
    }

    private static bool Covers(TestResult test, string table)
    {
        var thing = MermaidText.ShortClass(test.Class);
        return thing.EndsWith(TargetSuffix, StringComparison.Ordinal)
            && string.Equals(Normalize(thing[..^TargetSuffix.Length]), Normalize(table), StringComparison.Ordinal);
    }

    private static string Normalize(string name) => Separators().Replace(name, string.Empty).ToLowerInvariant();
}
