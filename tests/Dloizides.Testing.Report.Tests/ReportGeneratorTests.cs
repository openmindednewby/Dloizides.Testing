using Shouldly;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Generate", "Writes the run page, the runs index and latest.html for one run folder of .trx files.")]
[MethodUnderTest("Parse", "Reads the command line into report options, or an error and the usage line.")]
public sealed class ReportGeneratorTests : IDisposable
{
    private const string Project = "Shop.Tests";
    private readonly string reports = Path.Combine(Path.GetTempPath(), "test-report-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(reports))
            Directory.Delete(reports, true);
    }

    [Fact]
    public void Generate_WithFailingTest_ListsItFirstAndOpensItsFeature()
    {
        const string passing = "Shop.Tests.Alpha.AlphaTests.Load_WhenSaved_ReturnsIt";
        const string failing = "Shop.Tests.Beta.BetaTests.Save_WhenNew_ReturnsId";
        var run = WriteRun("20261007-100000", "Unit", new SampleResult("Shop.Tests.Alpha.AlphaTests", passing, "Passed"), new SampleResult("Shop.Tests.Beta.BetaTests", failing, "Failed"));

        var html = File.ReadAllText(ReportGenerator.Generate(Options(run)));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain($"<ol class=\"attn\"><li><a href=\"#t-1\">{failing}</a></li></ol>"),
            () => html.ShouldContain("<details class=\"feat\" open><summary><span class=\"fname\">Beta</span>"),
            () => html.IndexOf(">Beta<", StringComparison.Ordinal).ShouldBeLessThan(html.IndexOf(">Alpha<", StringComparison.Ordinal)));
    }

    [Fact]
    public void Generate_WithExpectedRedSet_MarksTheSetAmber()
    {
        var run = WriteRun("20261007-100000", "Target", new SampleResult("Shop.Tests.Beta.BetaTests", "Shop.Tests.Beta.BetaTests.Save_WhenNew_ReturnsId", "Failed"));

        var html = File.ReadAllText(ReportGenerator.Generate(Options(run) with { ExpectedRedSets = new HashSet<string> { "Target" } }));

        html.ShouldContain("<li class=\"set amber\"><span class=\"set-name\">Target</span><span class=\"set-verdict\">Red as expected: 1 targets are not built yet.</span>");
    }

    [Fact]
    public void Generate_WithTwoDatedRuns_PointsLatestAtTheNewest()
    {
        var older = WriteRun("20261006-090000", "Unit", new SampleResult("Shop.Tests.A.ATests", "Shop.Tests.A.ATests.Load_WhenSaved_ReturnsIt", "Passed"));
        var newer = WriteRun("20261007-100000", "Unit", new SampleResult("Shop.Tests.A.ATests", "Shop.Tests.A.ATests.Load_WhenSaved_ReturnsIt", "Passed"));
        ReportGenerator.Generate(Options(newer));

        ReportGenerator.Generate(Options(older));

        File.ReadAllText(Path.Combine(reports, "latest.html")).ShouldContain("url=20261007-100000/index.html");
    }

    [Fact]
    public void Generate_WithRunFolderWithoutIndex_LeavesItOffTheRunsIndex()
    {
        const string incomplete = "20261006-090000";
        var current = WriteRun("20261007-100000", "Unit", new SampleResult("Shop.Tests.A.ATests", "Shop.Tests.A.ATests.Load_WhenSaved_ReturnsIt", "Passed"));
        Directory.CreateDirectory(Path.Combine(reports, incomplete));

        ReportGenerator.Generate(Options(current));

        var index = File.ReadAllText(Path.Combine(reports, "index.html"));
        index.ShouldSatisfyAllConditions(
            () => index.ShouldContain("href=\"20261007-100000/index.html\""),
            () => index.ShouldNotContain(incomplete));
    }

    [Fact]
    public void Parse_WithAllOptions_ReadsEachOne()
    {
        string[] args = ["runs/20261007-100000", "--source", "src", "--expected-red", "Target, Next", "--set-order", "Unit,Target", "--name", "Shop"];

        var options = OptionsParser.Parse(args).Options!;

        options.ShouldSatisfyAllConditions(
            () => options.ExpectedRedSets.ShouldBe(new HashSet<string> { "Target", "Next" }, ignoreOrder: true),
            () => options.SetOrder.ShouldBe(["Unit", "Target"]),
            () => options.SourceRoots.ShouldBe([Path.GetFullPath("src")]),
            () => options.RunTitle.ShouldBe("Shop tests"));
    }

    [Fact]
    public void Parse_WithUnknownOption_ReturnsError()
    {
        string[] args = ["runs/20261007-100000", "--colour", "red"];

        var result = OptionsParser.Parse(args);

        (result.Options, result.Error).ShouldBe((null, "Unknown option \"--colour\"."));
    }

    private string WriteRun(string runName, string set, params SampleResult[] results)
    {
        var folder = Path.Combine(reports, runName);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, $"{set}-{Project}.trx"), TrxSample.Build(results));
        return folder;
    }

    private static ReportOptions Options(string runFolder) =>
        new(runFolder, [], new HashSet<string>(), [], string.Empty);
}
