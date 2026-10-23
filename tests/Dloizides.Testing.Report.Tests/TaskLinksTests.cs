using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Of", "Finds the task ids a test names in its description or name with the configured pattern, each id once.")]
[MethodUnderTest("Url", "Builds a task link from the configured URL pattern, the id escaped into its {id} slot.")]
[MethodUnderTest("Render", "Adds task chips, outcome, area and task filters and a by-task section to the run page when a task pattern is set.")]
[MethodUnderTest("Parse", "Reads --task-pattern and --task-url, refusing a pattern that is not a valid regular expression.")]
public class TaskLinksTests
{
    private const string Jira = "https://jira.example.com/browse/{id}";
    private const string EmsPattern = "EMS-[0-9]+";

    [Fact]
    public void Of_WithIdsInDescriptionAndName_ReturnsEachOnceInOrder()
    {
        var tasks = TaskLinks.Create(EmsPattern, string.Empty);
        var test = Test("ParkTests", "Load", "WhenSaved", TestStatus.Pass) with { Description = "Keeps park reads (EMS-1527, EMS-1527) after EMS-1626.", Name = "ParkTests.Load EMS-1" };

        var ids = tasks.Of(test);

        Assert.Equal(["EMS-1", "EMS-1527", "EMS-1626"], ids);
    }

    [Fact]
    public void Of_WithNoPattern_ReturnsNothing()
    {
        var test = Test("ParkTests", "Load", "WhenSaved", TestStatus.Pass) with { Description = "EMS-1527" };

        var ids = TaskLinks.None.Of(test);

        Assert.Empty(ids);
    }

    [Fact]
    public void Url_WithTemplate_PutsEscapedIdInItsSlot()
    {
        var tasks = TaskLinks.Create(EmsPattern, Jira);

        var plain = tasks.Url("EMS-1527");
        var spaced = tasks.Url("A B");

        Assert.Equal("https://jira.example.com/browse/EMS-1527", plain);
        Assert.Equal("https://jira.example.com/browse/A%20B", spaced);
    }

    [Fact]
    public void Url_WithoutTemplate_IsEmpty()
    {
        var tasks = TaskLinks.Create(EmsPattern, string.Empty);

        var url = tasks.Url("EMS-1");

        Assert.Equal(string.Empty, url);
    }

    [Fact]
    public void Render_WithTaskPattern_AddsFiltersChipsAndByTaskSection()
    {
        var run = TwoTestRun();
        var parts = new PageParts(SetLabels.None, RunDiagrams.None, TaskLinks.Create(EmsPattern, Jira));

        var html = RunPage.Render(run, "Trading tests", parts);

        Assert.Contains("<select id=\"fo\">", html, StringComparison.Ordinal);
        Assert.Contains("<select id=\"ff\">", html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"EMS-1527\">EMS-1527</option>", html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"-\">No task</option>", html, StringComparison.Ordinal);
        Assert.Contains("data-k=\"EMS-1527\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"https://jira.example.com/browse/EMS-1527\"", html, StringComparison.Ordinal);
        Assert.Contains("<section id=\"bytask\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithoutTaskPattern_HasAreaFilterButNoTaskFilterOrSection()
    {
        var run = TwoTestRun();

        var html = RunPage.Render(run, "Trading tests", SetLabels.None);

        Assert.Contains("<select id=\"ff\">", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"fk\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"bytask\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_WithTaskOptions_ReadsBoth()
    {
        var result = OptionsParser.Parse(["run", "--task-pattern", EmsPattern, "--task-url", Jira]);

        Assert.Equal(EmsPattern, result.Options?.TaskPattern);
        Assert.Equal(Jira, result.Options?.TaskUrl);
    }

    [Fact]
    public void Parse_WithInvalidTaskPattern_Fails()
    {
        var result = OptionsParser.Parse(["run", "--task-pattern", "EMS-[0-9"]);

        Assert.Null(result.Options);
        Assert.Contains("--task-pattern", result.Error, StringComparison.Ordinal);
    }

    private static TestRun TwoTestRun()
    {
        var set = new TestSet("Unit", false);
        set.Tests.Add(Test("ParkTests", "Load", "WhenSaved", TestStatus.Pass) with { Description = "Keeps park reads (EMS-1527)." });
        set.Tests.Add(Test("PortfolioTests", "Save", "WhenNew", TestStatus.Fail) with { Description = "Saves a portfolio." });
        return new TestRun("20261023-151141", null, [set], 0, string.Empty);
    }
}
