using System.Text.Json;

namespace Dloizides.Testing.Report;

internal sealed record RecordedCallLine(string Test, int Seq, string From, string To, string? Method, string? Path, int? Status);

internal static class RecordedCalls
{
    public const string Folder = "calls";

    public static TestRun Attach(TestRun run, string runFolder)
    {
        var calls = Read(Path.Combine(runFolder, Folder));
        return calls.Count == 0 ? run : run with { Sets = run.Sets.Select(set => AttachSet(set, calls)).ToList() };
    }

    private static TestSet AttachSet(TestSet set, Dictionary<string, IReadOnlyList<ResultsCall>> calls)
    {
        var attached = new TestSet(set.Name, set.ExpectRed);
        attached.Files.AddRange(set.Files);
        attached.Missing.AddRange(set.Missing);
        attached.Tests.AddRange(set.Tests.Select(test => calls.TryGetValue(BareName(test.Name), out var recorded) ? test with { Calls = recorded } : test));
        return attached;
    }

    private static Dictionary<string, IReadOnlyList<ResultsCall>> Read(string folder)
    {
        var byTest = new Dictionary<string, IReadOnlyList<ResultsCall>>(StringComparer.Ordinal);
        if (!Directory.Exists(folder))
            return byTest;
        foreach (var file in Directory.GetFiles(folder, "*.jsonl").Order(StringComparer.Ordinal))
        {
            var lines = File.ReadLines(file)
                .Where(line => line.Trim().Length > 0)
                .Select(line => JsonSerializer.Deserialize<RecordedCallLine>(line, ResultsJson.Options))
                .OfType<RecordedCallLine>()
                .Where(line => line.Test.Length > 0);
            foreach (var test in lines.GroupBy(line => line.Test, StringComparer.Ordinal))
                byTest.TryAdd(test.Key, test.OrderBy(line => line.Seq).Select(ToCall).ToList());
        }

        return byTest;
    }

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
