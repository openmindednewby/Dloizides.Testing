namespace Dloizides.Testing.Report.Tests;

internal static class DiagramSamples
{
    public static TestResult Test(string className, string method, string scenario, TestStatus status) => new()
    {
        Name = $"Shop.Tests.{className}.{method}_{scenario}_Works",
        Project = "Shop.Tests",
        Feature = "Shop",
        Class = className,
        Method = method,
        Description = string.Empty,
        Scenario = scenario,
        Expected = "Works",
        Args = string.Empty,
        Status = status,
        Seconds = 0,
        Message = string.Empty,
        Stack = string.Empty,
    };

    public static ResultsRequirement Requirement(string id, string title) => new() { Id = id, Title = title };
}
