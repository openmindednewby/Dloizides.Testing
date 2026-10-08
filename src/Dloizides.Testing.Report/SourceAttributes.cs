namespace Dloizides.Testing.Report;

internal sealed record RequirementRecord(string Id, string Text, string Path, string Class);

internal sealed record FlowEntry(string Name, int Step);

internal sealed record AttributeProblem(string Path, int Line, string Attribute, string Argument)
{
    public override string ToString() => $"{Path}:{Line}: [{Attribute}] argument {Argument} is not a literal, so the report cannot read it";
}

internal sealed record TestAttributes(IReadOnlyList<string> Covers, string? Feature, IReadOnlyList<FlowEntry> Flows);

internal sealed class AttributeSet
{
    public List<string> Covers { get; } = [];
    public List<FlowEntry> Flows { get; } = [];
    public string? Feature { get; set; }
}

internal sealed class SourceAttributes
{
    private static readonly AttributeSet None = new();

    public List<RequirementRecord> Requirements { get; } = [];
    public Dictionary<string, AttributeSet> Classes { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, AttributeSet> Methods { get; } = new(StringComparer.Ordinal);
    public List<AttributeProblem> Problems { get; } = [];

    public TestAttributes Test(string className, string method)
    {
        var onClass = Classes.GetValueOrDefault(className, None);
        var onMethod = Methods.GetValueOrDefault(MethodDescriptionReader.Key(className, method), None);
        return new TestAttributes(
            [.. onClass.Covers, .. onMethod.Covers],
            onMethod.Feature ?? onClass.Feature,
            [.. onClass.Flows, .. onMethod.Flows]);
    }

    public string FeatureOr(string className, string method, string fallback) =>
        Test(className, method).Feature ?? fallback;
}
