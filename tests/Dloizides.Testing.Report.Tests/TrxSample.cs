namespace Dloizides.Testing.Report.Tests;

internal sealed record SampleResult(string ClassName, string TestName, string Outcome, string Output = "", string Adapter = "");

internal static class TrxSample
{
    public const string Start = "2026-10-07T10:00:00.0000000+00:00";
    public const string Finish = "2026-10-07T10:01:30.0000000+00:00";

    public static string Build(params SampleResult[] results)
    {
        var rows = string.Concat(results.Select((r, i) =>
            $"<UnitTestResult testId=\"id-{i}\" testName=\"{r.TestName}\" outcome=\"{r.Outcome}\" duration=\"00:00:01.5000000\">{r.Output}</UnitTestResult>"));
        var definitions = string.Concat(results.Select((r, i) =>
            $"<UnitTest id=\"id-{i}\" name=\"{r.TestName}\"><TestMethod className=\"{r.ClassName}\" name=\"{MethodOf(r.TestName)}\"{AdapterOf(r)} /></UnitTest>"));
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?><TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">"
            + $"<Times start=\"{Start}\" finish=\"{Finish}\" /><Results>{rows}</Results><TestDefinitions>{definitions}</TestDefinitions></TestRun>";
    }

    private static string AdapterOf(SampleResult result) =>
        result.Adapter.Length > 0 ? $" adapterTypeName=\"{result.Adapter}\"" : string.Empty;

    private static string MethodOf(string testName)
    {
        var paren = testName.IndexOf('(', StringComparison.Ordinal);
        var bare = paren >= 0 ? testName[..paren] : testName;
        return bare[(bare.LastIndexOf('.') + 1)..];
    }
}
