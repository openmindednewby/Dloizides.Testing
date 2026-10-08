using Dloizides.Testing.Tests.Samples;

namespace Dloizides.Testing.Tests;

[MethodUnderTest("AC17", "Fails the shipped coverage guard naming every declared requirement that no test covers.")]
[MethodUnderTest("Undeclared", "Lists every [Covers] id that no [Requirement] in the test assembly declares.")]
public class RequirementCoverageGuardTests
{
    [Fact]
    public void AC17_WithDeclaredRequirementNeverCovered_FailsNamingIt()
    {
        string[] expected = ["Dloizides.Testing.Tests.Samples.RequirementSample: AC-01"];

        var uncovered = RequirementCoverage.Uncovered(typeof(RequirementSample).Assembly);

        Assert.Equal(expected, uncovered);
    }

    [Fact]
    public void Undeclared_WithCoversIdNoRequirementDeclares_ReportsIt()
    {
        string[] expected = ["Dloizides.Testing.Tests.Samples.RequirementSample.Submit_WhenLate_RejectsIt: AC-99"];

        var undeclared = RequirementCoverage.Undeclared(typeof(RequirementSample).Assembly);

        Assert.Equal(expected, undeclared);
    }
}
