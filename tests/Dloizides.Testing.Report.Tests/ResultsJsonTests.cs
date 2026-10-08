namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC12", "Rejects a results JSON file with a missing or unknown schema field and exits non-zero naming the field.")]
[MethodUnderTest("AC13", "Converts a trx run to results JSON and back into the HTML run page with the same tallies as the direct trx path.")]
public sealed class ResultsJsonTests : IDisposable
{
    private const int InvalidResults = 1;
    private const string Source = """
        [Requirement("AC-01", "Saved items load back")]
        public class AlphaTests
        {
            [Fact]
            [Covers("AC-01")]
            [Flow("Checkout", 2)]
            public void Load_WhenSaved_ReturnsIt() { }
        }
        """;

    private readonly string reports = Path.Combine(Path.GetTempPath(), "test-report-json-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(reports))
            Directory.Delete(reports, true);
    }

    [Theory]
    [InlineData("""{"run":{"name":"20261008-100000"},"requirements":[],"tests":[]}""", "testdoc-results: field \"schema\" is missing")]
    [InlineData("""{"schema":"testdoc-results.v2","run":{"name":"20261008-100000"},"requirements":[],"tests":[]}""", "testdoc-results: field \"schema\" has unknown major version \"testdoc-results.v2\"")]
    public void AC12_WithMissingOrUnknownSchema_ExitsNonZeroNamingTheField(string json, string expectedError)
    {
        var runFolder = Path.Combine(reports, "20261008-100000");
        Directory.CreateDirectory(runFolder);
        var resultsFile = Path.Combine(reports, "results.json");
        File.WriteAllText(resultsFile, json);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exit = Program.Run([runFolder, "--results", resultsFile], output, error);

        Assert.Equal(InvalidResults, exit);
        Assert.Contains(expectedError, error.ToString(), StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(runFolder, "index.html")));
    }

    [Fact]
    public void AC13_WithTrxRoundTrippedThroughJson_KeepsTodaysTallies()
    {
        var runFolder = Path.Combine(reports, "20261007-100000");
        Directory.CreateDirectory(runFolder);
        File.WriteAllText(Path.Combine(runFolder, "Unit-Shop.Tests.trx"), TrxSample.Build(
            new SampleResult("Shop.Tests.Alpha.AlphaTests", "Shop.Tests.Alpha.AlphaTests.Load_WhenSaved_ReturnsIt", "Passed"),
            new SampleResult("Shop.Tests.Beta.BetaTests", "Shop.Tests.Beta.BetaTests.Save_WhenNew_ReturnsId", "Failed"),
            new SampleResult("Shop.Tests.Beta.BetaTests", "Shop.Tests.Beta.BetaTests.Delete_WhenMissing_ReturnsFalse", "NotExecuted")));
        var options = new ReportOptions(runFolder, [], new HashSet<string>(), [], "Shop");
        var direct = new RunReader(options, new Dictionary<string, string>()).Read(runFolder);
        var directPage = RunPage.Render(direct, options.RunTitle, options.Labels);
        var attached = RunAttributes.Attach(direct, AttributeReader.Parse(Source, "/src/Shop.Tests/AlphaTests.cs"), ["/src"]);
        var tests = attached.Sets.Single().Tests;
        var alpha = tests.FindIndex(t => t.Name.EndsWith("Load_WhenSaved_ReturnsIt", StringComparison.Ordinal));
        tests[alpha] = tests[alpha] with { Calls = [new ResultsCall { Seq = 1, From = "Shop.Tests", To = "shop-api", Method = "GET", Path = "/items/1", Status = 200 }] };
        var directJson = ResultsJsonWriter.Write(attached);

        var roundTripped = ResultsJsonReader.Read(directJson);

        var loaded = roundTripped.Sets.Single().Tests.Single(t => t.Name.EndsWith("Load_WhenSaved_ReturnsIt", StringComparison.Ordinal));
        Assert.Equal(direct.Sets.Select(s => new Tally(s.Tests).Text()), roundTripped.Sets.Select(s => new Tally(s.Tests).Text()));
        Assert.Equal(3, roundTripped.Sets.Single().Tests.Count);
        Assert.Equal(directPage, RunPage.Render(roundTripped, options.RunTitle, options.Labels));
        Assert.Equal(directJson, ResultsJsonWriter.Write(roundTripped));
        Assert.Equal(["AC-01"], loaded.Covers);
        Assert.Equal(new FlowEntry("Checkout", 2), loaded.Flows.Single());
        Assert.Equal("/items/1", loaded.Calls.Single().Path);
        Assert.Equal(("AC-01", "Shop.Tests/AlphaTests.cs"), (roundTripped.Requirements.Single().Id, roundTripped.Requirements.Single().Source));
    }
}
