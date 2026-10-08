namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC07", "Gives a failing test in the expected-red set the amber xfail node class instead of red.")]
[MethodUnderTest("AC19", "Gives a passing test in the expected-red set (XPass) the red node class, the same as a failure.")]
public class StatusColourTests
{
    [Fact]
    public void AC07_WithFailingTestInExpectedRedSet_UsesAmberXfailClass()
    {
        const string given = "an expected-red set with a failing test";
        const string when = "the diagram is rendered";
        const string then = "node class is the amber xfail class, not red";

        Assert.Fail($"AC-07 not implemented. Given {given}; when {when}; then {then}.");
    }

    [Fact]
    public void AC19_WithPassingTestInExpectedRedSet_UsesRedClass()
    {
        const string given = "a test in an expected-red set that passes (XPass)";
        const string when = "the diagram is rendered";
        const string then = "node class is red, same as a failure";

        Assert.Fail($"AC-19 not implemented. Given {given}; when {when}; then {then}.");
    }
}
