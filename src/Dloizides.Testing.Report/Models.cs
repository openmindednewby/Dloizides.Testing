namespace Dloizides.Testing.Report;

internal sealed record TestResult
{
    public required string Name { get; init; }
    public required string Project { get; init; }
    public required string Feature { get; init; }
    public required string Class { get; init; }
    public required string Method { get; init; }
    public required string Description { get; init; }
    public required string Scenario { get; init; }
    public required string Expected { get; init; }
    public required string Args { get; init; }
    public required TestStatus Status { get; init; }
    public required double Seconds { get; init; }
    public required string Message { get; init; }
    public required string Stack { get; init; }
    public string Framework { get; init; } = ResultsJson.DefaultFramework;
    public IReadOnlyList<string> Covers { get; init; } = [];
    public IReadOnlyList<FlowEntry> Flows { get; init; } = [];
    public IReadOnlyList<ResultsCall> Calls { get; init; } = [];
    public IReadOnlyList<UseCaseEntry> UseCases { get; init; } = [];
}

internal sealed record TrxContext(string SetName, string Project, bool ExpectRed);

internal sealed record TrxFile(IReadOnlyList<TestResult> Tests, DateTimeOffset? Start, DateTimeOffset? Finish);

internal sealed record SetFile(string Project, string Trx, string Log);

internal sealed class TestSet(string name, bool expectRed)
{
    public string Name { get; } = name;
    public bool ExpectRed { get; } = expectRed;
    public List<SetFile> Files { get; } = [];
    public List<TestResult> Tests { get; } = [];
    public List<string> Missing { get; } = [];
}

internal sealed record TestRun(string Name, DateTime? Date, IReadOnlyList<TestSet> Sets, double Seconds, string SetFilter)
{
    public IReadOnlyList<ResultsRequirement> Requirements { get; init; } = [];
    public IReadOnlyList<ResultsFeature> Features { get; init; } = [];
    public string? Repo { get; init; }
    public string? Sha { get; init; }
    public string? StartedAt { get; init; }
    public string? FinishedAt { get; init; }

    public int UnattributedCalls { get; init; }
}

internal sealed record MethodNameParts(string Method, string Scenario, string Expected);
