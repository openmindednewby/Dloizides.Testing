using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC05", "Colours each declared requirement by the results of the tests that cover it, empty when none do.")]
[MethodUnderTest("AC06", "Draws a covered id with no matching [Requirement] in an undeclared group and counts it in the header.")]
public class RequirementMapTests
{
    [Fact]
    public void AC05_WithMixedCoverageResults_ColoursEachRequirementByItsTests()
    {
        ResultsRequirement[] requirements = [Requirement("AC-01", "A"), Requirement("AC-02", "B"), Requirement("AC-03", "C")];
        TestResult[] tests =
        [
            Test("SubmitTests", "Submit", "WhenValid", TestStatus.Pass) with { Covers = ["AC-01"] },
            Test("SubmitTests", "Submit", "WhenRepeated", TestStatus.Pass) with { Covers = ["AC-02"] },
            Test("SubmitTests", "Submit", "WhenLate", TestStatus.Fail) with { Covers = ["AC-02"] },
        ];

        var map = RequirementMap.Render(requirements, tests);

        Assert.Equal([("AC-01", "pass"), ("AC-02", "fail"), ("AC-03", "empty")], map.Cards.Select(c => (c.Id, c.Class)));
        Assert.Contains("r0[\"AC-01: A\"]:::pass", map.Mermaid, StringComparison.Ordinal);
        Assert.Contains("r1[\"AC-02: B\"]:::fail", map.Mermaid, StringComparison.Ordinal);
        Assert.Contains("r2[\"AC-03: C\"]:::empty", map.Mermaid, StringComparison.Ordinal);
        Assert.Contains("r1 --> m", map.Mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void AC06_WithCoveredIdNeverDeclared_DrawsItInUndeclaredGroup()
    {
        ResultsRequirement[] requirements = [Requirement("AC-01", "A")];
        TestResult[] tests = [Test("SubmitTests", "Submit", "WhenValid", TestStatus.Pass) with { Covers = ["AC-99"] }];

        var map = RequirementMap.Render(requirements, tests);

        Assert.Contains("subgraph undeclared[\"Undeclared ids (1)\"]\n        r1[\"AC-99\"]:::pass", map.Mermaid, StringComparison.Ordinal);
        Assert.Equal("1 requirement · 1 undeclared · 1 without tests", map.Header);
    }
}
