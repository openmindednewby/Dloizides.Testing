using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("FD1", "Heads each area with its feature name in words, the kind of thing under test as a tag and the code path in mono.")]
[MethodUnderTest("FD2", "Reads an assembly-level [Feature] with its Why, Context and Owner into the run and its results JSON.")]
[MethodUnderTest("FD3", "Draws the feature's Why card first inside its area, with Owner shown as not set when missing.")]
[MethodUnderTest("FD5", "Draws one use-case diagram per area from [UseCase], coloured by the results of the tests that carry it.")]
[MethodUnderTest("FD6", "Draws each flow an area belongs to inside that area while keeping the top-level flow section.")]
[MethodUnderTest("FD7", "Orders an area as Why, use cases, flow, collapsed sequence of recorded calls, then scenarios.")]
public class FeatureDocsTests
{
    private const StringComparison Ordinal = StringComparison.Ordinal;

    [Fact]
    public void FD1_WithOneClassArea_ShowsTitleKindTagAndMonoPath()
    {
        var battery = new ResultsFeature { Name = "Battery availability", Why = "Shows the capacity.", Context = "Called by HENEX.", Owner = string.Empty };
        var html = Page([], BatteryTest("WithAuth", TestStatus.Pass) with { Feature = "BatteryAvailability" });

        Assert.Contains("<span class=\"gname\"><span class=\"ftitle\">Battery availability</span><span class=\"kind\">Endpoint</span></span>", html, Ordinal);
        Assert.Contains("<span class=\"gpath\">BatteryAvailabilityEndpointTests.BatteryAvailability</span>", html, Ordinal);
        Assert.DoesNotContain("<span class=\"type\">", html, Ordinal);
    }

    [Fact]
    public void FD1_WithTwoClassArea_TitlesEachClassInWordsWithoutItsKind()
    {
        var html = Page(
            [],
            Test("AdmieIspResultsParserTests", "Parse", "WhenValid", TestStatus.Pass),
            Test("AdmieRepositoryTests", "Save", "WhenNew", TestStatus.Pass));

        Assert.Contains("<span class=\"ftitle\">Admie isp results</span><span class=\"kind\">Parser</span>", html, Ordinal);
        Assert.Contains("<span class=\"gpath\">AdmieIspResultsParserTests.Parse</span>", html, Ordinal);
    }

    [Fact]
    public void FD2_WithAssemblyFeature_ReadsWhyContextAndOwner()
    {
        var battery = new ResultsFeature { Name = "Battery availability", Why = "Shows the capacity.", Context = "Called by HENEX.", Owner = string.Empty };
        const string source = """
            [assembly: Feature("Battery availability", Why = "Shows the capacity.", Context = "Called by HENEX.", Owner = "Trading")]
            namespace Shop.Tests;
            public class BatteryTests { }
            """;
        ResultsFeature[] expected = [battery with { Owner = "Trading" }];

        var read = AttributeReader.Parse(source, "Shop.Tests/FeatureInfo.cs");

        Assert.Equal(expected, read.Features);
        Assert.Empty(read.Problems);
        Assert.Null(read.Test("BatteryTests", "Load").Feature);
    }

    [Fact]
    public void FD2_WithFeatureAndUseCaseInRun_RoundTripsThroughResultsJson()
    {
        var battery = new ResultsFeature { Name = "Battery availability", Why = "Shows the capacity.", Context = "Called by HENEX.", Owner = string.Empty };
        var run = Run([battery], BatteryTest("WithAuth", TestStatus.Pass) with { UseCases = [UseCase("Get capacity", "API caller")] });

        var back = ResultsJsonReader.Read(ResultsJsonWriter.Write(run));

        Assert.Equal([battery], back.Features);
        Assert.Equal([UseCase("Get capacity", "API caller")], back.Sets[0].Tests[0].UseCases);
    }

    [Fact]
    public void FD3_WithDeclaredFeature_DrawsWhyCardFirstWithOwnerNotSet()
    {
        var battery = new ResultsFeature { Name = "Battery availability", Why = "Shows the capacity.", Context = "Called by HENEX.", Owner = string.Empty };
        var html = Page([battery], BatteryTest("WithAuth", TestStatus.Pass) with { Feature = "BatteryAvailability", Description = "Returns capacity per slot." });

        Assert.Contains("<div class=\"whycard\"><dl><dt>Why</dt><dd>Shows the capacity.</dd><dt>Context</dt><dd>Called by HENEX.</dd><dt>Owner</dt><dd class=\"unset\">not set</dd></dl></div>", html, Ordinal);
        Assert.InRange(html.IndexOf("class=\"whycard\"", Ordinal), 0, html.IndexOf("<p class=\"desc\">", Ordinal));
    }

    [Fact]
    public void FD3_WithoutDeclaredFeature_DrawsNoWhyCard()
    {
        var html = Page([], BatteryTest("WithAuth", TestStatus.Pass));

        Assert.DoesNotContain("class=\"whycard\"", html, Ordinal);
    }

    [Fact]
    public void FD5_WithUseCases_DrawsAreaDiagramColouredByCoveringTests()
    {
        var run = Run(
            [],
            BatteryTest("WithZeroStep", TestStatus.Skip) with { UseCases = [UseCase("Get capacity per slot", "API caller")] },
            BatteryTest("WithUnknown", TestStatus.Pass) with { UseCases = [UseCase("Get 404 for an unknown portfolio", "API caller")] });

        var diagram = RunDiagrams.Build(run, null).UseCases.Single();

        Assert.Contains("a1[\"API caller\"]:::actor", diagram.Mermaid, Ordinal);
        Assert.Contains("u1([\"Get capacity per slot\"]):::skip", diagram.Mermaid, Ordinal);
        Assert.Contains("u2([\"Get 404 for an unknown portfolio\"]):::pass", diagram.Mermaid, Ordinal);
        Assert.Contains("a1 --- u2", diagram.Mermaid, Ordinal);
    }

    [Fact]
    public void FD5_WithoutUseCases_DrawsNoUseCaseDiagram()
    {
        var run = Run([], BatteryTest("WithAuth", TestStatus.Pass));

        var diagrams = RunDiagrams.Build(run, null);

        Assert.Empty(diagrams.UseCases);
        Assert.DoesNotContain("class=\"usecases\"", RunPage.Render(run, "Shop tests", SetLabels.None, diagrams), Ordinal);
    }

    [Fact]
    public void FD6_WithAreasInFlow_DrawsTheFlowInsideEachAreaAndKeepsTopLevel()
    {
        var html = Page(
            [],
            Test("FetchTests", "Fetch", "WhenDue", TestStatus.Pass) with { Feature = "Fetch", Flows = [new FlowEntry("Import", 1)] },
            Test("ParserTests", "Parse", "WhenValid", TestStatus.Pass) with { Feature = "Parse", Flows = [new FlowEntry("Import", 2)] });

        Assert.Contains("<figure class=\"diagram\" id=\"f-1-import\">", html, Ordinal);
        Assert.Equal(2, Count(html, "<div class=\"areaflow\">"));
        Assert.Equal(3, Count(html, "subgraph flow["));
    }

    [Fact]
    public void FD7_WithEverySection_OrdersWhyUseCasesFlowSequenceScenarios()
    {
        var battery = new ResultsFeature { Name = "Battery availability", Why = "Shows the capacity.", Context = "Called by HENEX.", Owner = string.Empty };
        var test = BatteryTest("WithAuth", TestStatus.Pass) with
        {
            UseCases = [UseCase("Get capacity", "API caller")],
            Flows = [new FlowEntry("Import", 1)],
            Calls = [new ResultsCall { Seq = 1, From = "Caller", To = "AccountController", Method = "GET", Path = "/battery", Status = 200 }],
        };

        var html = Page([battery], test);

        int[] at = [Index(html, "class=\"whycard\""), Index(html, "class=\"usecases\""), Index(html, "<div class=\"areaflow\">"), Index(html, "<details class=\"seq\">"), Index(html, "<table>")];
        Assert.DoesNotContain(-1, at);
        Assert.Equal(at.Order(), at);
        Assert.Contains("p1-&gt;&gt;p2: GET /battery", html, Ordinal);
    }

    [Fact]
    public void FD7_WithoutRecordedCalls_DrawsNoSequence()
    {
        var battery = new ResultsFeature { Name = "Battery availability", Why = "Shows the capacity.", Context = "Called by HENEX.", Owner = string.Empty };
        var html = Page([battery], BatteryTest("WithAuth", TestStatus.Pass));

        Assert.DoesNotContain("<details class=\"seq\">", html, Ordinal);
    }

    private static UseCaseEntry UseCase(string text, string actor) => new() { Text = text, Actor = actor };

    private static TestResult BatteryTest(string scenario, TestStatus status) =>
        Test("BatteryAvailabilityEndpointTests", "BatteryAvailability", scenario, status) with { Feature = "Battery availability" };

    private static TestRun Run(IReadOnlyList<ResultsFeature> features, params TestResult[] tests)
    {
        var set = new TestSet("Unit", false);
        set.Tests.AddRange(tests);
        return new TestRun("20261007-100000", null, [set], 0, string.Empty) { Features = features };
    }

    private static string Page(IReadOnlyList<ResultsFeature> features, params TestResult[] tests)
    {
        var run = Run(features, tests);
        return RunPage.Render(run, "Shop tests", SetLabels.None, RunDiagrams.Build(run, null));
    }

    private static int Index(string html, string fragment) => html.IndexOf(fragment, Ordinal);

    private static int Count(string html, string fragment)
    {
        var count = 0;
        for (var at = html.IndexOf(fragment, Ordinal); at >= 0; at = html.IndexOf(fragment, at + 1, Ordinal))
            count++;
        return count;
    }
}
