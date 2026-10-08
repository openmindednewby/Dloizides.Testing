using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Render", "Renders the run page from test results, every test-supplied string HTML-encoded.")]
[MethodUnderTest("TypeOf", "Derives the thing-under-test badge from the last word of the class name before Tests.")]
public class RunPageTests
{
    [Fact]
    public void Render_WithHostileNameAndMessage_EncodesThem()
    {
        var set = new TestSet("Unit", false);
        set.Tests.Add(Test("ATests", "Load", "WhenSaved", TestStatus.Fail) with { Name = "<script>alert(1)</script>", Message = "<img src=x onerror=alert(2)>" });
        var run = new TestRun("20261007-100000", null, [set], 0, string.Empty);

        var html = RunPage.Render(run, "Shop tests", SetLabels.None);

        Assert.DoesNotContain("<script>alert(1)", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<img src=x", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithTwoClassesAtNextFlowStep_FeedsLinksBoth()
    {
        var set = new TestSet("Unit", false);
        set.Tests.Add(Test("FetchJobTests", "Fetch", "WhenDue", TestStatus.Pass) with { Flows = [new FlowEntry("Import", 1)] });
        set.Tests.Add(Test("ParserTests", "Parse", "WhenValid", TestStatus.Pass) with { Flows = [new FlowEntry("Import", 2)] });
        set.Tests.Add(Test("MapperTests", "Map", "WhenValid", TestStatus.Pass) with { Flows = [new FlowEntry("Import", 2)] });
        var run = new TestRun("20261007-100000", null, [set], 0, string.Empty);

        var html = RunPage.Render(run, "Shop tests", SetLabels.None, RunDiagrams.Build(run, null));

        Assert.Contains("Feeds:&nbsp;<a href=\"#c-shop-tests-parsertests\">Parser &#9656;</a> <a href=\"#c-shop-tests-mappertests\">Mapper &#9656;</a>", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("BatterySubmitTests", "Submit")]
    [InlineData("AdmieIspResultsParserTests", "Parser")]
    [InlineData("ParksCommercialTargetTests", "Schema target")]
    [InlineData("Outer+InnerEndpointTests", "Endpoint")]
    [InlineData("ParserTests", "Parser")]
    [InlineData("HTTPClientTests", "Client")]
    [InlineData("ApiHTTPTests", "HTTP")]
    [InlineData("Tests", "Test")]
    public void TypeOf_WithClassName_UsesItsLastWord(string className, string badge)
    {
        var type = Badges.TypeOf(className);

        Assert.Equal(badge, type);
    }
}
