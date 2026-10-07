using System.Reflection;
using Dloizides.Testing.Tests.Samples;
using Shouldly;

namespace Dloizides.Testing.Tests;

[MethodUnderTest("Constructor", "Keeps the tested method name and its business description exactly as written.")]
[MethodUnderTest("Usage", "Allows several descriptions on one test class and only on classes.")]
public class MethodUnderTestAttributeTests
{
    [Fact]
    public void Constructor_WithMethodAndDescription_ExposesBoth()
    {
        const string method = "Load";
        const string description = "Loads the persisted value back by its id.";

        var attribute = new MethodUnderTestAttribute(method, description);

        attribute.ShouldSatisfyAllConditions(
            () => attribute.Method.ShouldBe(method),
            () => attribute.Description.ShouldBe(description));
    }

    [Fact]
    public void Usage_WhenTwoAppliedToOneClass_ReadsBoth()
    {
        string[] expected = ["Sum", "Round"];

        var methods = typeof(TwoDescriptionsSample).GetCustomAttributes<MethodUnderTestAttribute>().Select(a => a.Method);

        methods.ShouldBe(expected, ignoreOrder: true);
    }

    [Fact]
    public void Usage_WhenInspected_TargetsClassesOnly()
    {
        const AttributeTargets expected = AttributeTargets.Class;

        var usage = typeof(MethodUnderTestAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;

        usage.ValidOn.ShouldBe(expected);
    }
}
