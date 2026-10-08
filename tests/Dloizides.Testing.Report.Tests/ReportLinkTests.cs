namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC20", "Prints one stable latest report path a task doc can link to and writes nothing outside the report folder.")]
public class ReportLinkTests
{
    [Fact]
    public void AC20_WithRunWrittenToReportFolder_PrintsStableLatestPath()
    {
        const string given = "a run written to a report folder";
        const string when = "test-report finishes";
        const string then = "it prints one stable latest report path a task doc can link to and writes nothing outside the report folder";

        Assert.Fail($"AC-20 not implemented. Given {given}; when {when}; then {then}.");
    }
}
