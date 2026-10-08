using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Render", "Folds every single-child level into one row and states the status once, on the outermost row.")]
public class RunTreeTests
{
    private const string Separator = "<span class=\"sep\"> › </span>";

    [Fact]
    public void Render_WithOneClassOneMethod_FoldsAreaClassAndMethodIntoOneRow()
    {
        var html = Page(
            Battery("WithAuth", TestStatus.Pass),
            Battery("WithoutAuth", TestStatus.Pass),
            Battery("WithEmptyPortfolio", TestStatus.Pass),
            Battery("WithZeroStep", TestStatus.Skip));

        Assert.Equal(1, Count(html, "<details class=\"grp"));
        Assert.Contains("<span class=\"gname\"><span class=\"ftitle\">Battery availability</span><span class=\"kind\">Endpoint</span></span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"gpath\">BatteryAvailabilityEndpointTests.BatteryAvailability</span>", html, StringComparison.Ordinal);
        Assert.Contains("<span class=\"tcount\">4 scenarios</span>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithTwoClassesInArea_StatesStatusOnlyOnTheAreaRow()
    {
        var html = Page(
            Test("SubmitTests", "Submit", "WhenValid", TestStatus.Fail) with { Feature = "Shop" },
            Test("ViewTests", "View", "WhenSaved", TestStatus.Pass) with { Feature = "Shop" });

        Assert.Contains("<details class=\"grp area\" id=\"a-1-shop\"><summary><span class=\"tw\"></span><span class=\"gh\"><span class=\"gname\"><span class=\"ftitle\">Shop</span><span class=\"kind\">Flow &#183; 2 classes</span></span>", html, StringComparison.Ordinal);
        Assert.Equal(1, Count(html, "<span class=\"chips\">"));
        Assert.Equal(2, Count(html, "<details class=\"grp thing"));
    }

    [Fact]
    public void Render_WithOnlyPassingTests_ShowsOnlyNonZeroCounts()
    {
        var html = Page(Test("SubmitTests", "Submit", "WhenValid", TestStatus.Pass), Test("SubmitTests", "Submit", "WhenRepeated", TestStatus.Pass));

        Assert.Contains("<span class=\"chips\"><span class=\"chip pass\"><span class=\"sw\"></span>2 passed</span></span>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("0 failed", html, StringComparison.Ordinal);
        Assert.DoesNotContain("0 expected red", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithSkipReason_ShowsItAsSubLineOfItsScenario()
    {
        var html = Page(Test("SubmitTests", "Submit", "WhenLate", TestStatus.Skip) with { Message = "Divides by zero" });

        Assert.Contains("WhenLate<span class=\"why skip\">Skipped: Divides by zero</span></td>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<tr class=\"why", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithCoveredRequirement_DrawsItsMapInsideTheClassGroup()
    {
        var html = Page(
            Test("SubmitTests", "Submit", "WhenValid", TestStatus.Pass) with { Project = "Shop.Tests", Feature = "Shop", Covers = ["AC-01"] },
            Test("ViewTests", "View", "WhenSaved", TestStatus.Pass) with { Project = "Shop.Tests", Feature = "Shop" });

        var group = html.IndexOf("id=\"c-shop-tests-submittests\"", StringComparison.Ordinal);
        var map = html.IndexOf("<details class=\"rmap\"><summary>Requirement map", StringComparison.Ordinal);
        Assert.InRange(map, group, html.IndexOf("id=\"c-shop-tests-viewtests\"", StringComparison.Ordinal));
        Assert.Contains("href=\"requirements-c-shop-tests-submittests.html\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"requirements.html\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithFlowStep_LinksAreaToFlowAndFlowStepsToGroups()
    {
        var html = Page(
            Test("FetchTests", "Fetch", "WhenDue", TestStatus.Pass) with { Project = "Shop.Tests", Feature = "Fetch", Flows = [new FlowEntry("Import", 1)] },
            Test("ParserTests", "Parse", "WhenValid", TestStatus.Pass) with { Project = "Shop.Tests", Feature = "Parse", Flows = [new FlowEntry("Import", 2)] });

        Assert.Contains("<p class=\"partof\">Part of: <a href=\"#f-1-import\">Import</a>, step 2 of 2</p>", html, StringComparison.Ordinal);
        Assert.Contains("<figure class=\"diagram\" id=\"f-1-import\">", html, StringComparison.Ordinal);
        Assert.Contains("<li><a href=\"#c-shop-tests-parsertests\">Step 2: Parser.Parse</a></li>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithSchemaTargetArea_LinksToTheDatabaseDiagram()
    {
        const string snapshot = "modelBuilder.Entity(\"Shop.A\", b => { b.ToTable(\"a\"); });";
        var set = new TestSet("MigrationTarget", true);
        set.Tests.Add(Test("ATargetTests", "Columns", "WhenMigrated", TestStatus.XFail) with { Feature = "Migration" });
        var run = new TestRun("20261007-100000", null, [set], 0, string.Empty);

        var html = RunPage.Render(run, "Shop tests", SetLabels.None, RunDiagrams.Build(run, snapshot));

        Assert.Contains("<p class=\"partof\"><a href=\"#schema-h\">Database diagram</a></p>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithFailingClassInsideArea_MarksItsInnerRowWithARedDot()
    {
        var html = Page(
            Test("SubmitTests", "Submit", "WhenValid", TestStatus.Fail) with { Project = "Shop.Tests", Feature = "Shop" },
            Test("ViewTests", "View", "WhenSaved", TestStatus.Pass) with { Project = "Shop.Tests", Feature = "Shop" });

        Assert.Contains("<details class=\"grp thing\" id=\"c-shop-tests-submittests\"><summary><span class=\"tw\"></span><span class=\"gh\"><span class=\"dot red\"", html, StringComparison.Ordinal);
        Assert.Contains("<details class=\"grp thing\" id=\"c-shop-tests-viewtests\"><summary><span class=\"tw\"></span><span class=\"gh\"><span class=\"gname\">", html, StringComparison.Ordinal);
        Assert.Equal(1, Count(html, "<span class=\"chips\">"));
    }

    [Fact]
    public void Render_WithMessageAndNoStack_SummarisesTheNoteAsFullMessage()
    {
        var html = Page(Test("SubmitTests", "Submit", "WhenValid", TestStatus.Fail) with { Message = "Expected 2" });

        Assert.Contains("<summary>Full message</summary>", html, StringComparison.Ordinal);
    }

    private static TestResult Battery(string scenario, TestStatus status) =>
        Test("BatteryAvailabilityEndpointTests", "BatteryAvailability", scenario, status) with { Feature = "Battery availability" };

    private static string Page(params TestResult[] tests)
    {
        var set = new TestSet("Unit", false);
        set.Tests.AddRange(tests);
        var run = new TestRun("20261007-100000", null, [set], 0, string.Empty);
        return RunPage.Render(run, "Shop tests", SetLabels.None, RunDiagrams.Build(run, null));
    }

    private static int Count(string html, string fragment)
    {
        var count = 0;
        for (var at = html.IndexOf(fragment, StringComparison.Ordinal); at >= 0; at = html.IndexOf(fragment, at + 1, StringComparison.Ordinal))
            count++;
        return count;
    }
}
