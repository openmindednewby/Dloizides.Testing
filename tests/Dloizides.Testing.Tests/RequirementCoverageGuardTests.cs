using Dloizides.Testing.Tests.Samples;

namespace Dloizides.Testing.Tests;

[MethodUnderTest("AC17", "Fails the shipped coverage guard naming every declared requirement that no test covers.")]
[MethodUnderTest("Undeclared", "Lists every [Covers] id that no [Requirement] on its own class declares.")]
public class RequirementCoverageGuardTests
{
    [Fact]
    public void AC17_WithDeclaredRequirementNeverCovered_FailsNamingIt()
    {
        string[] expected =
        [
            "Dloizides.Testing.Tests.Samples.RequirementSample: AC-01",
            "Dloizides.Testing.Tests.Samples.RequirementWithoutTestsSample: AC-03",
            "Dloizides.Testing.Tests.Samples.ScopedRequirementSample: AC-02",
        ];

        var uncovered = RequirementCoverage.Uncovered(typeof(RequirementSample).Assembly);

        Assert.Equal(expected, uncovered);
    }

    [Fact]
    public void Undeclared_WithCoversIdNoRequirementDeclares_ReportsIt()
    {
        string[] expected =
        [
            "Dloizides.Testing.Tests.Samples.ForeignCoversSample.Submit_WhenForeign_ReportsIt: AC-01",
            "Dloizides.Testing.Tests.Samples.RequirementSample.Submit_WhenLate_RejectsIt: AC-99",
        ];

        var undeclared = RequirementCoverage.Undeclared(typeof(RequirementSample).Assembly);

        Assert.Equal(expected, undeclared);
    }

    [Fact]
    public void AC17_WithSameIdCoveredOnlyInAnotherClass_FailsNamingIt()
    {
        const string expected = "Dloizides.Testing.Tests.Samples.ScopedRequirementSample: AC-02";

        var uncovered = RequirementCoverage.Uncovered(typeof(ScopedRequirementSample).Assembly);

        Assert.Contains(expected, uncovered);
    }

    [Fact]
    public void AC17_WithRequirementOnClassWithoutTests_FailsNamingIt()
    {
        const string expected = "Dloizides.Testing.Tests.Samples.RequirementWithoutTestsSample: AC-03";

        var uncovered = RequirementCoverage.Uncovered(typeof(RequirementWithoutTestsSample).Assembly);

        Assert.Contains(expected, uncovered);
    }

    [Fact]
    public void Undeclared_WithIdDeclaredOnlyByAnotherClass_ReportsIt()
    {
        const string expected = "Dloizides.Testing.Tests.Samples.ForeignCoversSample.Submit_WhenForeign_ReportsIt: AC-01";

        var undeclared = RequirementCoverage.Undeclared(typeof(ForeignCoversSample).Assembly);

        Assert.Contains(expected, undeclared);
    }
}
