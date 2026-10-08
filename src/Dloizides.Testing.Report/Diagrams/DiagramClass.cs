namespace Dloizides.Testing.Report;

internal static class DiagramClass
{
    public const string Pass = "pass";
    public const string Fail = "fail";
    public const string Skip = "skip";
    public const string XFail = "xfail";
    public const string Empty = "empty";
    public const string Plain = "plain";

    public static readonly IReadOnlyList<(string Name, string Style)> Definitions =
    [
        (Pass, "fill:#1d7348,stroke:#1d7348,color:#ffffff"),
        (Fail, "fill:#b3261e,stroke:#b3261e,color:#ffffff"),
        (Skip, "fill:#6b7785,stroke:#6b7785,color:#ffffff"),
        (XFail, "fill:#9a5b06,stroke:#9a5b06,color:#ffffff"),
        (Empty, "stroke:#6b7785,stroke-dasharray:4 3"),
        (Plain, "stroke:#556372"),
    ];

    public static string For(TestStatus status) => status switch
    {
        TestStatus.Fail or TestStatus.XPass => Fail,
        TestStatus.Skip => Skip,
        TestStatus.XFail => XFail,
        _ => Pass,
    };

    public static string ForWorst(IEnumerable<TestResult> tests, string none)
    {
        var statuses = tests.Select(t => t.Status).ToList();
        return statuses.Count == 0 ? none : For(statuses.Min());
    }

    public static string ClassDefs(string indent) =>
        string.Concat(Definitions.Select(d => $"{indent}classDef {d.Name} {d.Style}\n"));
}
