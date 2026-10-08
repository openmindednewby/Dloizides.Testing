namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC20", "Prints one stable latest report path a task doc can link to and writes nothing outside the report folder.")]
public sealed class ReportLinkTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "test-report-link-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }

    [Fact]
    public void AC20_WithRunWrittenToReportFolder_PrintsStableLatestPath()
    {
        var reports = Path.Combine(root, "reports");
        var run = Path.Combine(reports, "20261007-100000");
        Directory.CreateDirectory(run);
        File.WriteAllText(Path.Combine(run, "Unit-Shop.Tests.trx"), TrxSample.Build(new SampleResult("Shop.Tests.A.ATests", "Shop.Tests.A.ATests.Load_WhenSaved_ReturnsIt", "Passed")));
        using var output = new StringWriter();

        var exit = Program.Run([run], output, TextWriter.Null);

        var latest = output.ToString().Split(Environment.NewLine).Where(l => l.StartsWith("Latest: ", StringComparison.Ordinal));
        Assert.Equal(0, exit);
        Assert.Equal([$"Latest: {Path.Combine(reports, "latest.html")}"], latest);
        Assert.Contains("url=20261007-100000/index.html", File.ReadAllText(Path.Combine(reports, "latest.html")), StringComparison.Ordinal);
        Assert.Equal([reports], Directory.GetFileSystemEntries(root));
    }
}
