namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC08", "Draws one node per flow step in step order, grey for a step whose only test was skipped.")]
public class FlowDiagramTests
{
    [Fact]
    public void AC08_WithStepTwoOnlySkipped_DrawsThreeOrderedNodesWithStepTwoGrey()
    {
        const string given = "a flow with steps 1-3, step 2 has a skipped test only";
        const string when = "the flow is rendered";
        const string then = "three nodes in step order, step 2 grey";

        Assert.Fail($"AC-08 not implemented. Given {given}; when {when}; then {then}.");
    }
}
