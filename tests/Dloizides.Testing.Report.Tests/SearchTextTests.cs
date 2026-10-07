using Shouldly;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Build", "Builds the lower-cased words a test row can be found by.")]
public sealed class SearchTextTests
{
    [Fact]
    public void Build_WithPascalCaseMethod_SplitsItIntoWords()
    {
        var text = SearchText.Build(Result(method: "GetPortfolio_WhenMissing_Returns404", scenario: "with non existing portfolio", expected: "returns 404"));

        text.ShouldSatisfyAllConditions(
            () => text.ShouldContain("get portfolio"),
            () => text.ShouldContain("non existing portfolio"),
            () => text.ShouldContain("returns 404"));
    }

    [Fact]
    public void Build_WithFailureMessage_IncludesItLowerCased()
    {
        var text = SearchText.Build(Result(message: "Expected 404 But Was 500"));

        text.ShouldContain("expected 404 but was 500");
    }

    [Fact]
    public void Build_WithDescriptionAndFeature_IncludesBoth()
    {
        var text = SearchText.Build(Result(description: "Loads The Portfolio", feature: "Portfolios"));

        text.ShouldSatisfyAllConditions(
            () => text.ShouldContain("loads the portfolio"),
            () => text.ShouldContain("portfolios"));
    }

    private static TestResult Result(string method = "M", string scenario = "", string expected = "", string message = "", string description = "", string feature = "F") =>
        new()
        {
            Name = "N.M", Project = "P", Feature = feature, Class = "C", Method = method, Description = description,
            Scenario = scenario, Expected = expected, Args = string.Empty, Status = TestStatus.Pass, Seconds = 0, Message = message, Stack = string.Empty,
        };
}
