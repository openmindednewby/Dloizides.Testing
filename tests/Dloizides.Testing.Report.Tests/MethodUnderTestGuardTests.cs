using Shouldly;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Missing", "Fails the build of the report tool when one of its test classes leaves a prefix undescribed.")]
[MethodUnderTest("Invalid", "Fails the build of the report tool when one of its descriptions is stale or too short.")]
public class MethodUnderTestGuardTests
{
    [Fact]
    public void Missing_WithThisTestAssembly_IsEmpty() =>
        MethodUnderTestCoverage.Missing(typeof(MethodUnderTestGuardTests).Assembly).ShouldBeEmpty();

    [Fact]
    public void Invalid_WithThisTestAssembly_IsEmpty() =>
        MethodUnderTestCoverage.Invalid(typeof(MethodUnderTestGuardTests).Assembly).ShouldBeEmpty();
}
