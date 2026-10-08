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
    public void Generate_WithFailingTest_ListsItFirstWithItsAreaClosed()
    {
        const string passing = "Shop.Tests.Alpha.AlphaTests.Load_WhenSaved_ReturnsIt";
        const string failing = "Shop.Tests.Beta.BetaTests.Save_WhenNew_ReturnsId";
        var run = WriteRun("20261007-100000", "Unit", new SampleResult("Shop.Tests.Alpha.AlphaTests", passing, "Passed"), new SampleResult("Shop.Tests.Beta.BetaTests", failing, "Failed"));

        var html = File.ReadAllText(ReportGenerator.Generate(Options(run)).RunPage);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain($"<ol class=\"attn\"><li><a href=\"#t-1\">{failing}</a></li></ol>"),
            () => html.ShouldContain("<details class=\"grp area\" id=\"a-1-beta\"><summary>"),
            () => html.ShouldNotContain("<details class=\"grp area\" open"),
            () => html.IndexOf(">Beta<", StringComparison.Ordinal).ShouldBeLessThan(html.IndexOf(">Alpha<", StringComparison.Ordinal)));
    }

    [Fact]
    public void Generate_WithFeatureClass_ShowsAreaThenThingWithTypeBadge()
    {
        var run = WriteRun("20261007-100000", "Unit", new SampleResult("Shop.Tests.Battery.BatteryEndpointTests", "Shop.Tests.Battery.BatteryEndpointTests.Load_WhenSaved_ReturnsIt", "Passed"));

        var html = File.ReadAllText(ReportGenerator.Generate(Options(run)).RunPage);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("<span class=\"gname\">Battery<span class=\"sep\"> › </span>BatteryEndpoint<span class=\"type\">Endpoint</span>"),
            () => html.ShouldContain("<span class=\"sep\"> › </span><span class=\"mname\">Load</span></span>"));
    }

    [Fact]
    public void Generate_WithoutFlowAttributes_ShowsNoFeedsOrSteps()
    {
        var run = WriteRun("20261007-100000", "Unit", new SampleResult("Shop.Tests.Battery.BatteryEndpointTests", "Shop.Tests.Battery.BatteryEndpointTests.Load_WhenSaved_ReturnsIt", "Passed"));

        var html = File.ReadAllText(ReportGenerator.Generate(Options(run)).RunPage);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldNotContain("Feeds:"),
            () => html.ShouldNotContain("class=\"flow\""),
            () => html.ShouldNotContain("class=\"mermaid\""));
    }

    [Fact]
    public void Generate_WithFlowAttributes_LinksEachStepToTheNextAndWritesTheDiagram()
    {
        var source = Path.Combine(reports, "src");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "ImportTests.cs"), """
            public class FetchJobTests
            {
                [Fact]
                [Flow("Import", 1)]
                public void Fetch_WhenDue_Downloads() { }
            }

            public class ParserTests
            {
                [Fact]
                [Flow("Import", 2)]
                public void Parse_WhenValid_ReadsRows() { }
            }
            """);
        var run = WriteRun(
            "20261007-100000",
            "Unit",
            new SampleResult("Shop.Tests.Import.FetchJobTests", "Shop.Tests.Import.FetchJobTests.Fetch_WhenDue_Downloads", "Passed"),
            new SampleResult("Shop.Tests.Import.ParserTests", "Shop.Tests.Import.ParserTests.Parse_WhenValid_ReadsRows", "Passed"));

        var html = File.ReadAllText(ReportGenerator.Generate(Options(run) with { SourceRoots = [source] }).RunPage);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("Feeds:&nbsp;<a href=\"#c-shop-tests-parsertests\">Parser &#9656;</a>"),
            () => html.ShouldContain("Part of: <a href=\"#f-1-import\">Import</a>, step 1 of 2"),
            () => html.ShouldContain("<div class=\"mermaid\">flowchart LR"),
            () => html.ShouldContain("<a href=\"flow-1-import.html\">Open full size</a>"),
            () => File.ReadAllText(Path.Combine(run, "flow-1-import.mmd")).ShouldContain("s1 --> s2"));
    }

    [Fact]
    public void Generate_WithExpectedRedSet_MarksTheSetAmber()
    {
        var run = WriteRun("20261007-100000", "Target", new SampleResult("Shop.Tests.Beta.BetaTests", "Shop.Tests.Beta.BetaTests.Save_WhenNew_ReturnsId", "Failed"));

        var html = File.ReadAllText(ReportGenerator.Generate(Options(run) with { ExpectedRedSets = new HashSet<string> { "Target" } }).RunPage);

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
    public void Parse_WithEfSnapshot_ReadsItsFullPath()
    {
        string[] args = ["runs/20261007-100000", "--ef-snapshot", "Migrations/ShopDbContextModelSnapshot.cs"];

        var options = OptionsParser.Parse(args).Options!;

        options.EfSnapshot.ShouldBe(Path.GetFullPath("Migrations/ShopDbContextModelSnapshot.cs"));
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
