namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC05", "Colours each declared requirement by the results of the tests that cover it, empty when none do.")]
[MethodUnderTest("AC06", "Draws a covered id with no matching [Requirement] in an undeclared group and counts it in the header.")]
public class RequirementMapTests
{
    [Fact]
    public void AC05_WithMixedCoverageResults_ColoursEachRequirementByItsTests()
    {
        const string given = "requirements AC-01..AC-03 declared; tests covering AC-01 (pass) and AC-02 (one pass, one fail)";
        const string when = "the requirement map is rendered";
        const string then = "AC-01 green, AC-02 red, AC-03 empty box";

        Assert.Fail($"AC-05 not implemented. Given {given}; when {when}; then {then}.");
    }

    [Fact]
    public void AC06_WithCoveredIdNeverDeclared_DrawsItInUndeclaredGroup()
    {
        const string given = "a test with [Covers('AC-99')] and no [Requirement('AC-99', ...)] anywhere";
        const string when = "the requirement map is rendered";
        const string then = "AC-99 drawn in an undeclared group and counted in the header";

        Assert.Fail($"AC-06 not implemented. Given {given}; when {when}; then {then}.");
    }
}
