namespace Dloizides.Testing.Tests;

[MethodUnderTest("AC16", "Records every call through a wrapped fake server in order and draws one sequence arrow per call.")]
public class RecordingHandlerTests
{
    [Fact]
    public void AC16_WithTwoCallsThroughFakeServer_RecordsAndDrawsBothInOrder()
    {
        const string given = "a fake HENEX server wrapped in RecordingHandler, the test makes two calls";
        const string when = "the test runs and the report is rendered";
        const string then = "JSON calls has 2 entries in order; sequence diagram has 2 arrows";

        Assert.Fail($"AC-16 not implemented. Given {given}; when {when}; then {then}.");
    }
}
