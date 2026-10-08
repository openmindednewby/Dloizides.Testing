using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC07", "Gives a failing test in the expected-red set the amber xfail node class instead of red.")]
[MethodUnderTest("AC19", "Gives a passing test in the expected-red set (XPass) the red node class, the same as a failure.")]
public class StatusColourTests
{
    [Fact]
    public void AC07_WithFailingTestInExpectedRedSet_UsesAmberXfailClass()
    {
        var status = StatusText.FromOutcome("Failed", expectRed: true);
        TestResult[] tests = [Test("ParksTargetTests", "ParksColumns", "WhenMerged", status) with { Covers = ["AC-01"] }];

        var map = RequirementMap.Render([Requirement("AC-01", "A")], tests);

        Assert.Contains("r0[\"AC-01: A\"]:::xfail", map.Mermaid, StringComparison.Ordinal);
        Assert.Contains("classDef xfail fill:#9a5b06", map.Mermaid, StringComparison.Ordinal);
        Assert.DoesNotContain(":::fail", map.Mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void AC19_WithPassingTestInExpectedRedSet_UsesRedClass()
    {
        var status = StatusText.FromOutcome("Passed", expectRed: true);
        TestResult[] tests = [Test("ParksTargetTests", "ParksColumns", "WhenMerged", status) with { Covers = ["AC-01"] }];

        var map = RequirementMap.Render([Requirement("AC-01", "A")], tests);

        Assert.Contains("r0[\"AC-01: A\"]:::fail", map.Mermaid, StringComparison.Ordinal);
        Assert.Contains("classDef fail fill:#b3261e", map.Mermaid, StringComparison.Ordinal);
        Assert.DoesNotContain(":::pass", map.Mermaid, StringComparison.Ordinal);
    }
}
