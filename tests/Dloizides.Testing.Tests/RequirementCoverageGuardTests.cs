namespace Dloizides.Testing.Tests;

[MethodUnderTest("AC17", "Fails the shipped coverage guard naming every declared requirement that no test covers.")]
public class RequirementCoverageGuardTests
{
    [Fact]
    public void AC17_WithDeclaredRequirementNeverCovered_FailsNamingIt()
    {
        const string given = "[Requirement('AC-01','A')] declared but no test covers it";
        const string when = "the shipped coverage guard runs";
        const string then = "it fails naming AC-01";

        Assert.Fail($"AC-17 not implemented. Given {given}; when {when}; then {then}.");
    }
}
