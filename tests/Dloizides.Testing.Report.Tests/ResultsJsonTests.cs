namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC12", "Rejects a results JSON file with a missing or unknown schema field and exits non-zero naming the field.")]
[MethodUnderTest("AC13", "Converts a trx run to results JSON and back into the HTML run page with the same tallies as the direct trx path.")]
public class ResultsJsonTests
{
    [Fact]
    public void AC12_WithMissingOrUnknownSchema_ExitsNonZeroNamingTheField()
    {
        const string given = "a JSON file without schema or with testdoc-results.v2";
        const string when = "test-report reads it";
        const string then = "exits non-zero naming the field";

        Assert.Fail($"AC-12 not implemented. Given {given}; when {when}; then {then}.");
    }

    [Fact]
    public void AC13_WithTrxRoundTrippedThroughJson_KeepsTodaysTallies()
    {
        const string given = "a trx run";
        const string when = "it is converted to JSON and back into the existing HTML run page";
        const string then = "same tallies as the current direct-trx path";

        Assert.Fail($"AC-13 not implemented. Given {given}; when {when}; then {then}.");
    }
}
