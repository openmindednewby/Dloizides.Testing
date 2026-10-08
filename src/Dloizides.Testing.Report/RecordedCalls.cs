using System.Text.Json;

namespace Dloizides.Testing.Report;

internal sealed record RecordedCallLine(string Test, int Seq, string From, string To, string? Method, string? Path, int? Status)
{
    public int? Case { get; init; }

    public string? Run { get; init; }

    public string? Framework { get; init; }
}

internal static class RecordedCalls
{
    public const string Folder = "calls";
    public const string Unattributed = "(unattributed)";

    public static TestRun Attach(TestRun run, string runFolder)
    {
        var lines = Read(Path.Combine(runFolder, Folder));
        if (lines.Count == 0)
            return run;
        var calls = lines.Where(IsAttributed)
            .GroupBy(line => line.Test, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, Pick, StringComparer.Ordinal);
        return run with { UnattributedCalls = lines.Count(line => !IsAttributed(line)), Sets = run.Sets.Select(set => AttachSet(set, calls)).ToList() };
    }

    private static bool IsAttributed(RecordedCallLine line) => line.Test.Length > 0 && line.Test != Unattributed;

    private static IReadOnlyList<ResultsCall> Pick(IEnumerable<RecordedCallLine> lines) =>
        lines.GroupBy(line => (Run: line.Run ?? string.Empty, Framework: line.Framework ?? string.Empty, Case: line.Case ?? 0))
            .OrderByDescending(group => group.Key.Run, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Framework, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Case)
            .First()
            .OrderBy(line => line.Seq)
            .Select(ToCall)
            .ToList();

    private static TestSet AttachSet(TestSet set, Dictionary<string, IReadOnlyList<ResultsCall>> calls)
    {
        var attached = new TestSet(set.Name, set.ExpectRed);
        attached.Files.AddRange(set.Files);
        attached.Missing.AddRange(set.Missing);
        var drawn = new HashSet<string>(StringComparer.Ordinal);
        attached.Tests.AddRange(set.Tests.Select(test =>
            calls.TryGetValue(BareName(test.Name), out var recorded) && drawn.Add(BareName(test.Name)) ? test with { Calls = recorded } : test));
        return attached;
    }

    private static List<RecordedCallLine> Read(string folder) =>
        !Directory.Exists(folder)
            ? []
            : Directory.GetFiles(folder, "*.jsonl")
                .Order(StringComparer.Ordinal)
                .SelectMany(File.ReadLines)
                .Where(line => line.Trim().Length > 0)
                .Select(line => JsonSerializer.Deserialize<RecordedCallLine>(line, ResultsJson.Options))
                .OfType<RecordedCallLine>()
                .ToList();

    private static ResultsCall ToCall(RecordedCallLine line) => new()
    {
        Seq = line.Seq,
        From = line.From,
        To = line.To,
        Method = line.Method,
        Path = line.Path,
        Status = line.Status,
    };

    private static string BareName(string testName)
    {
        var paren = testName.IndexOf('(', StringComparison.Ordinal);
        return paren >= 0 ? testName[..paren] : testName;
    }
}
