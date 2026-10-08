using System.Text.Json;
using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Attach", "Attaches each test's recorded calls from the newest run, one theory case on its first row, and counts the calls no test owns.")]
[MethodUnderTest("Page", "Says on the run page how many recorded calls no test owns.")]
public sealed class RecordedCallsTests : IDisposable
{
    private const string TestName = "Shop.Tests.AlphaTests.Load_WhenSaved_ReturnsIt";
    private readonly string runFolder = Path.Combine(Path.GetTempPath(), "testdoc-attach-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Attach_WithTwoRunsOfOneTest_TakesTheNewestRun()
    {
        WriteCalls("a.calls.jsonl", Line(TestName, 0, "/new", "2026-10-09T10:00:00.0000000Z"));
        WriteCalls("b.calls.jsonl", Line(TestName, 0, "/old", "2026-10-09T09:00:00.0000000Z"));

        var run = RecordedCalls.Attach(RunOf(TestName), runFolder);

        Assert.Equal(["/new"], run.Sets[0].Tests[0].Calls.Select(call => call.Path));
    }

    [Fact]
    public void Attach_WithTheoryRows_DrawsFirstCaseOnFirstRowOnly()
    {
        WriteCalls("a.calls.jsonl", Line(TestName, 1, "/one", "2026-10-09T10:00:00.0000000Z"), Line(TestName, 2, "/two", "2026-10-09T10:00:00.0000000Z"));

        var run = RecordedCalls.Attach(RunOf(TestName + "(input: 1)", TestName + "(input: 2)"), runFolder);

        Assert.Equal(["/one"], run.Sets[0].Tests[0].Calls.Select(call => call.Path));
        Assert.Empty(run.Sets[0].Tests[1].Calls);
    }

    [Fact]
    public void Attach_WithUnattributedCalls_CountsThem()
    {
        WriteCalls("a.calls.jsonl", Line(RecordedCalls.Unattributed, 0, "/a", "2026-10-09T10:00:00.0000000Z"), Line(string.Empty, 0, "/b", "2026-10-09T10:00:00.0000000Z"));

        var run = RecordedCalls.Attach(RunOf(TestName), runFolder);

        Assert.Equal(2, run.UnattributedCalls);
    }

    [Fact]
    public void Page_WithUnattributedCalls_SaysHowMany()
    {
        var run = RunOf(TestName) with { UnattributedCalls = 2 };

        var html = RunPage.Render(run, "Shop tests", SetLabels.None);

        Assert.Contains("2 calls not attributed to a test", html, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(runFolder))
            Directory.Delete(runFolder, recursive: true);
    }

    private static string Line(string test, int testCase, string path, string run) =>
        JsonSerializer.Serialize(new { test, @case = testCase, seq = 1, from = "Shop", to = "HENEX", method = "GET", path, status = 200, run, framework = ".NET 10.0.0" });

    private static TestRun RunOf(params string[] names)
    {
        var set = new TestSet("Unit", false);
        set.Tests.AddRange(names.Select(name => Test("AlphaTests", "Load", "WhenSaved", TestStatus.Pass) with { Name = name }));
        return new TestRun("20261009-100000", null, [set], 0, string.Empty);
    }

    private void WriteCalls(string file, params string[] lines)
    {
        var folder = Path.Combine(runFolder, RecordedCalls.Folder);
        Directory.CreateDirectory(folder);
        File.WriteAllLines(Path.Combine(folder, file), lines);
    }
}
