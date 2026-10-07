using System.Reflection;
using Dloizides.Testing.Tests.Samples;
using Shouldly;

namespace Dloizides.Testing.Tests;

[MethodUnderTest("Missing", "Lists every test-name prefix whose class carries no business description of it.")]
[MethodUnderTest("Invalid", "Lists every description that names no tested method or is too short to explain it.")]
public class MethodUnderTestCoverageTests
{
    private static readonly Assembly Samples = typeof(UndescribedSample).Assembly;

    [Fact]
    public void Missing_WhenPrefixHasNoAttribute_ReportsClassAndPrefix()
    {
        const string expected = "UndescribedSample: Save";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldContain(expected);
    }

    [Fact]
    public void Missing_WhenEveryPrefixIsDescribed_ReportsNothingForThatClass()
    {
        string[] describedClasses = ["DescribedSample:", "TwoDescriptionsSample:"];

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldNotContain(entry => describedClasses.Any(prefix => entry.StartsWith(prefix, StringComparison.Ordinal)));
    }

    [Fact]
    public void Missing_WithTheoryMethod_TreatsItAsATest()
    {
        const string expected = "TheorySample: Parse";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldContain(expected);
    }

    [Fact]
    public void Missing_WithFactSubclass_TreatsItAsATest()
    {
        const string expected = "CustomFactSample: Fetch";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldContain(expected);
    }

    [Fact]
    public void Missing_WithNonXunitAttributeNamedFact_TreatsItAsATest()
    {
        const string expected = "NameOnlyFactSample: Count";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldContain(expected);
    }

    [Fact]
    public void Missing_WithAbstractClass_SkipsIt()
    {
        const string abstractClass = "AbstractSample:";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldNotContain(entry => entry.StartsWith(abstractClass, StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_WithSampleAssembly_ReturnsExactlyTheUndescribedPrefixesInOrder()
    {
        string[] expected =
        [
            "CustomFactSample: Fetch",
            "NameOnlyFactSample: Count",
            "StaleDescriptionSample: Archive",
            "TheorySample: Parse",
            "UndescribedSample: Save",
        ];

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldBe(expected);
    }

    [Fact]
    public void Missing_WithNullAssembly_ThrowsArgumentNull()
    {
        Assembly assembly = null!;

        var act = () => MethodUnderTestCoverage.Missing(assembly);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void Invalid_WhenDescriptionNamesNoTestedMethod_ReportsIt()
    {
        const string expected = "StaleDescriptionSample: Delete";

        var invalid = MethodUnderTestCoverage.Invalid(Samples);

        invalid.ShouldContain(expected);
    }

    [Fact]
    public void Invalid_WhenDescriptionIsShorterThanDefaultMinimum_ReportsIt()
    {
        const string expected = "ShortDescriptionSample: Merge";

        var invalid = MethodUnderTestCoverage.Invalid(Samples);

        invalid.ShouldContain(expected);
    }

    [Fact]
    public void Invalid_WithMinimumBelowDescriptionLength_AcceptsShortDescription()
    {
        const int minLength = 5;
        const string shortDescription = "ShortDescriptionSample: Merge";

        var invalid = MethodUnderTestCoverage.Invalid(Samples, minLength);

        invalid.ShouldNotContain(shortDescription);
    }

    [Fact]
    public void Invalid_WithSampleAssembly_ReturnsExactlyTheBrokenDescriptionsInOrder()
    {
        string[] expected = ["ShortDescriptionSample: Merge", "StaleDescriptionSample: Delete"];

        var invalid = MethodUnderTestCoverage.Invalid(Samples);

        invalid.ShouldBe(expected);
    }

    [Fact]
    public void Invalid_WithNegativeMinimum_ThrowsArgumentOutOfRange()
    {
        const int minLength = -1;

        var act = () => MethodUnderTestCoverage.Invalid(Samples, minLength);

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }
}
