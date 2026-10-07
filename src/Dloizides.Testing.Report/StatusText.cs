namespace Dloizides.Testing.Report;

internal static class StatusText
{
    public static readonly TestStatus[] CountOrder = [TestStatus.Fail, TestStatus.XPass, TestStatus.XFail, TestStatus.Skip, TestStatus.Pass];

    public static readonly TestStatus[] BarOrder = [TestStatus.Pass, TestStatus.XFail, TestStatus.Skip, TestStatus.XPass, TestStatus.Fail];

    public static string Css(TestStatus status) => status switch
    {
        TestStatus.Fail => "fail",
        TestStatus.XPass => "xpass",
        TestStatus.Skip => "skip",
        TestStatus.XFail => "xfail",
        _ => "pass",
    };

    public static string Label(TestStatus status) => status switch
    {
        TestStatus.Fail => "Failed",
        TestStatus.XPass => "Passed early",
        TestStatus.Skip => "Skipped",
        TestStatus.XFail => "Expected red",
        _ => "Passed",
    };

    public static string CountWord(TestStatus status) => status switch
    {
        TestStatus.Fail => "failed",
        TestStatus.XPass => "passed early",
        TestStatus.Skip => "skipped",
        TestStatus.XFail => "expected red",
        _ => "passed",
    };

    public static TestStatus FromOutcome(string outcome, bool expectRed)
    {
        if (outcome == "NotExecuted")
            return TestStatus.Skip;
        if (expectRed)
            return outcome switch
            {
                "Passed" => TestStatus.XPass,
                "Failed" => TestStatus.XFail,
                _ => TestStatus.Fail,
            };
        return outcome == "Passed" ? TestStatus.Pass : TestStatus.Fail;
    }
}
