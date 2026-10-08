using System.Text.Json;

namespace Dloizides.Testing;

internal sealed record CallLine(string Test, int Seq, string From, string To, string Method, string Path, int Status);

/// <summary>Appends recorded calls as JSON lines to one file per log, for test-report to draw.</summary>
public sealed class CallLog
{
    /// <summary>Environment variable naming the folder the shared log writes to.</summary>
    public const string DirectoryVariable = "TESTDOC_CALLS_DIR";

    private const string DefaultFolder = "testdoc-calls";

    private static readonly JsonSerializerOptions LineOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly Lazy<CallLog> SharedLog = new(() => new CallLog(SharedDirectory()));

    private readonly object gate = new();
    private readonly Dictionary<string, int> counters = new(StringComparer.Ordinal);
    private readonly string directory;

    /// <summary>Writes to a new file inside <paramref name="directory"/>.</summary>
    public CallLog(string directory)
    {
        this.directory = directory;
        FilePath = Path.Combine(directory, $"{Guid.NewGuid():N}.calls.jsonl");
    }

    /// <summary>The default log: the TESTDOC_CALLS_DIR folder, else testdoc-calls beside the test assembly.</summary>
    public static CallLog Shared => SharedLog.Value;

    /// <summary>The JSON-lines file this log appends to.</summary>
    public string FilePath { get; }

    internal int Next(string test)
    {
        lock (gate)
        {
            var seq = counters.GetValueOrDefault(test) + 1;
            counters[test] = seq;
            return seq;
        }
    }

    internal void Write(CallLine line)
    {
        var text = JsonSerializer.Serialize(line, LineOptions) + "\n";
        lock (gate)
        {
            Directory.CreateDirectory(directory);
            File.AppendAllText(FilePath, text);
        }
    }

    private static string SharedDirectory() =>
        Environment.GetEnvironmentVariable(DirectoryVariable) is { Length: > 0 } configured
            ? configured
            : Path.Combine(AppContext.BaseDirectory, DefaultFolder);
}
