using Shouldly;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Split", "Turns a Method_Scenario_Expected test name into the method, scenario and expectation shown in the report.")]
[MethodUnderTest("ToWords", "Turns a PascalCase identifier into readable lower-case words, keeping acronyms and numbers.")]
public class MethodNameSplitterTests
{
    [Fact]
    public void Split_WithThreeParts_ReturnsMethodScenarioAndExpected()
    {
        const string name = "Save_WhenNew_ReturnsGeneratedId";

        var parts = MethodNameSplitter.Split(name);

        parts.ShouldBe(new MethodNameParts("Save", "When new", "Returns generated id"));
    }

    [Fact]
    public void Split_WithTwoParts_LeavesScenarioEmpty()
    {
        const string name = "Load_ReturnsSlots";

        var parts = MethodNameSplitter.Split(name);

        parts.ShouldBe(new MethodNameParts("Load", string.Empty, "Returns slots"));
    }

    [Fact]
    public void Split_WithoutUnderscore_ReturnsWholeNameAsMethod()
    {
        const string name = "Smoke";

        var parts = MethodNameSplitter.Split(name);

        parts.ShouldBe(new MethodNameParts("Smoke", string.Empty, string.Empty));
    }

    [Fact]
    public void Split_WithSeveralScenarioParts_JoinsThemWithCommas()
    {
        const string name = "Save_WhenNew_WithoutName_Throws";

        var parts = MethodNameSplitter.Split(name);

        parts.Scenario.ShouldBe("When new, without name");
    }

    [Fact]
    public void ToWords_WithAcronymAndNumber_KeepsBothReadable()
    {
        const string identifier = "ReturnsHTTPStatus201";

        var words = MethodNameSplitter.ToWords(identifier);

        words.ShouldBe("Returns HTTP status 201");
    }

    [Fact]
    public void ToWords_WithEmptyIdentifier_ReturnsEmpty()
    {
        const string identifier = "";

        var words = MethodNameSplitter.ToWords(identifier);

        words.ShouldBeEmpty();
    }
}
