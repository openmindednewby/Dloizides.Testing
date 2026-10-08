using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dloizides.Testing.Report;

internal sealed class ResultsDocument
{
    public string Schema { get; init; } = ResultsJson.SchemaName;
    public ResultsRun Run { get; init; } = new();
    public IReadOnlyList<ResultsRequirement> Requirements { get; init; } = [];
    public IReadOnlyList<ResultsTest> Tests { get; init; } = [];
}

internal sealed class ResultsRun
{
    public string Name { get; init; } = string.Empty;
    public string? Repo { get; init; }
    public string? Sha { get; init; }
    public double Seconds { get; init; }
    public string? StartedAt { get; init; }
    public string? FinishedAt { get; init; }
    public string SetFilter { get; init; } = string.Empty;
    public IReadOnlyList<ResultsSet> Sets { get; init; } = [];
}

internal sealed class ResultsSet
{
    public string Name { get; init; } = string.Empty;
    public bool ExpectRed { get; init; }
    public IReadOnlyList<ResultsFile> Files { get; init; } = [];
    public IReadOnlyList<string> Missing { get; init; } = [];
}

internal sealed class ResultsFile
{
    public string Project { get; init; } = string.Empty;
    public string Trx { get; init; } = string.Empty;
    public string Log { get; init; } = string.Empty;
}

internal sealed record ResultsRequirement
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
}

internal sealed class ResultsFlow
{
    public string Name { get; init; } = string.Empty;
    public int Step { get; init; }
}

internal sealed record ResultsCall
{
    public int Seq { get; init; }
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string? Method { get; init; }
    public string? Path { get; init; }
    public int? Status { get; init; }
}

internal sealed class ResultsTest
{
    public string Id { get; init; } = string.Empty;
    public string Framework { get; init; } = ResultsJson.DefaultFramework;
    public string Project { get; init; } = string.Empty;
    public string Set { get; init; } = string.Empty;
    public bool ExpectRed { get; init; }
    public string Feature { get; init; } = string.Empty;
    public string Class { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Scenario { get; init; } = string.Empty;
    public string Expected { get; init; } = string.Empty;
    public string Args { get; init; } = string.Empty;
    public IReadOnlyList<string> Covers { get; init; } = [];
    public IReadOnlyList<ResultsFlow> Flows { get; init; } = [];
    public string Status { get; init; } = string.Empty;
    public double Seconds { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Stack { get; init; } = string.Empty;
    public IReadOnlyList<ResultsCall> Calls { get; init; } = [];
}

internal sealed class ResultsJsonException(string field, string problem) : Exception($"testdoc-results: field \"{field}\" {problem}");

internal static class ResultsJson
{
    public const string SchemaName = "testdoc-results.v1";
    public const string SchemaPrefix = "testdoc-results.v";
    public const string SupportedMajor = "1";
    public const string FileName = "testdoc-results.v1.json";
    public const string DefaultFramework = "xunit";

    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private static readonly Dictionary<string, TestStatus> Statuses = new(StringComparer.Ordinal)
    {
        ["fail"] = TestStatus.Fail,
        ["xpass"] = TestStatus.XPass,
        ["skip"] = TestStatus.Skip,
        ["xfail"] = TestStatus.XFail,
        ["pass"] = TestStatus.Pass,
    };

    public static string StatusName(TestStatus status) => Statuses.First(pair => pair.Value == status).Key;

    public static bool TryStatus(string name, out TestStatus status) => Statuses.TryGetValue(name, out status);
}
