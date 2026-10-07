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
        const string expected = "Dloizides.Testing.Tests.Samples.UndescribedSample: Save";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldContain(expected);
    }

    [Fact]
    public void Missing_WhenEveryPrefixIsDescribed_ReportsNothingForThatClass()
    {
        string[] describedClasses = ["Dloizides.Testing.Tests.Samples.DescribedSample:", "Dloizides.Testing.Tests.Samples.TwoDescriptionsSample:"];

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldNotContain(entry => describedClasses.Any(prefix => entry.StartsWith(prefix, StringComparison.Ordinal)));
    }

    [Fact]
    public void Missing_WithTheoryMethod_TreatsItAsATest()
    {
        const string expected = "Dloizides.Testing.Tests.Samples.TheorySample: Parse";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldContain(expected);
    }

    [Fact]
    public void Missing_WithFactSubclass_TreatsItAsATest()
    {
        const string expected = "Dloizides.Testing.Tests.Samples.CustomFactSample: Fetch";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldContain(expected);
    }

    [Fact]
    public void Missing_WithNonXunitAttributeNamedFact_TreatsItAsATest()
    {
        const string expected = "Dloizides.Testing.Tests.Samples.NameOnlyFactSample: Count";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldContain(expected);
    }

    [Fact]
    public void Missing_WithStaticFactMethod_TreatsItAsATest()
    {
        const string expected = "Dloizides.Testing.Tests.Samples.StaticFactSample: Compute";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldContain(expected);
    }

    [Fact]
    public void Missing_WhenBaseClassDescribesInheritedTest_ReportsNothingForDerivedClass()
    {
        const string derivedClass = "Dloizides.Testing.Tests.Samples.InheritedDescriptionSample:";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldNotContain(entry => entry.StartsWith(derivedClass, StringComparison.Ordinal));
    }

    [Fact]
    public void Invalid_WhenBaseClassDescribesInheritedTest_ReportsNothingForDerivedClass()
    {
        const string derivedClass = "Dloizides.Testing.Tests.Samples.InheritedDescriptionSample:";

        var invalid = MethodUnderTestCoverage.Invalid(Samples);

        invalid.ShouldNotContain(entry => entry.StartsWith(derivedClass, StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_WithAbstractClass_SkipsIt()
    {
        const string abstractClass = "Dloizides.Testing.Tests.Samples.AbstractSample:";

        var missing = MethodUnderTestCoverage.Missing(Samples);

        missing.ShouldNotContain(entry => entry.StartsWith(abstractClass, StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_WithSampleAssembly_ReturnsExactlyTheUndescribedPrefixesInOrder()
    {
        string[] expected =
        [
            "Dloizides.Testing.Tests.Samples.CustomFactSample: Fetch",
            "Dloizides.Testing.Tests.Samples.NameOnlyFactSample: Count",
            "Dloizides.Testing.Tests.Samples.StaleDescriptionSample: Archive",
            "Dloizides.Testing.Tests.Samples.StaticFactSample: Compute",
            "Dloizides.Testing.Tests.Samples.TheorySample: Parse",
            "Dloizides.Testing.Tests.Samples.UndescribedSample: Save",
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
        const string expected = "Dloizides.Testing.Tests.Samples.StaleDescriptionSample: Delete";

        var invalid = MethodUnderTestCoverage.Invalid(Samples);

        invalid.ShouldContain(expected);
    }

    [Fact]
    public void Invalid_WhenDescriptionIsShorterThanDefaultMinimum_ReportsIt()
    {
        const string expected = "Dloizides.Testing.Tests.Samples.ShortDescriptionSample: Merge";

        var invalid = MethodUnderTestCoverage.Invalid(Samples);

        invalid.ShouldContain(expected);
    }

    [Fact]
    public void Invalid_WithMinimumBelowDescriptionLength_AcceptsShortDescription()
    {
        const int minLength = 5;
        const string shortDescription = "Dloizides.Testing.Tests.Samples.ShortDescriptionSample: Merge";

        var invalid = MethodUnderTestCoverage.Invalid(Samples, minLength);

        invalid.ShouldNotContain(shortDescription);
    }

    [Fact]
    public void Invalid_WithSampleAssembly_ReturnsExactlyTheBrokenDescriptionsInOrder()
    {
        string[] expected = ["Dloizides.Testing.Tests.Samples.ShortDescriptionSample: Merge", "Dloizides.Testing.Tests.Samples.StaleDescriptionSample: Delete"];

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
