namespace Dloizides.Testing.Report;

internal sealed record RequirementRecord(string Id, string Text, string Path, string Class);

internal sealed record FlowEntry(string Name, int Step);

internal sealed record AttributeProblem(string Path, int Line, string Attribute, string Argument, string Reason = AttributeProblem.NotLiteral)
{
    public const string Braces = "braces";
    public const string NotLiteral = "not literal";
    public const string BadId = "bad id";

    public override string ToString() => (Attribute, Reason) switch
    {
        (Braces, _) => $"{Path}:{Line}: unbalanced braces ({Argument}), so attributes after this line may be filed under the wrong class",
        (_, BadId) => $"{Path}:{Line}: [{Attribute}] id \"{Argument}\" breaks the id grammar {IdGrammar.Pattern}, so the report cannot link it",
        _ => $"{Path}:{Line}: [{Attribute}] argument {Argument} is not a literal, so the report cannot read it",
    };
}

internal sealed record TestAttributes(IReadOnlyList<string> Covers, string? Feature, IReadOnlyList<FlowEntry> Flows, IReadOnlyList<UseCaseEntry> UseCases);

internal sealed class AttributeSet
{
    public List<string> Covers { get; } = [];
    public List<FlowEntry> Flows { get; } = [];
    public List<UseCaseEntry> UseCases { get; } = [];
    public string? Feature { get; set; }
}

internal sealed class SourceAttributes
{
    private static readonly AttributeSet None = new();

    public List<RequirementRecord> Requirements { get; } = [];
    public List<ResultsFeature> Features { get; } = [];
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
            [.. onClass.Flows, .. onMethod.Flows],
            [.. onClass.UseCases, .. onMethod.UseCases]);
    }

    public string FeatureOr(string className, string method, string fallback) =>
        Test(className, method).Feature ?? fallback;
}
