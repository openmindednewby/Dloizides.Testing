using System.Reflection;
using Dloizides.Testing.Tests.Samples;

namespace Dloizides.Testing.Tests;

[MethodUnderTest("FD2", "An assembly-level [Feature] carries the feature's Why, Context and Owner next to its name.")]
[MethodUnderTest("FD4", "Lists every area that has tests but no assembly-level [Feature] with a Why, so consumers can assert the list is empty.")]
public class FeatureGuardTests
{
    private static readonly Assembly Samples = typeof(DescribedSample).Assembly;

    [Fact]
    public void FD2_WithAssemblyFeature_ExposesWhyContextAndOwner()
    {
        var feature = Samples.GetCustomAttributes<FeatureAttribute>().Single(f => f.Name == "Described sample");

        Assert.Equal(("Loads persisted values back by id.", "Sample only.", "Testing"), (feature.Why, feature.Context, feature.Owner));
    }

    [Fact]
    public void FD4_WithAreaWithoutFeature_ListsIt()
    {
        var missing = FeatureGuard.MissingFeatures(Samples);

        Assert.Contains("UndescribedSample", missing);
    }

    [Fact]
    public void FD4_WithAreaDeclaredByAssemblyFeature_DoesNotListIt()
    {
        var missing = FeatureGuard.MissingFeatures(Samples);

        Assert.DoesNotContain("DescribedSample", missing);
    }

    [Fact]
    public void FD4_WithClassFeatureNeverDeclared_ListsItsName()
    {
        var missing = FeatureGuard.MissingFeatures(Samples);

        Assert.Contains("Checkout", missing);
    }

    [Fact]
    public void FD4_WithDeclaredFeatureWithoutWhy_ListsIt()
    {
        var missing = FeatureGuard.MissingFeatures(Samples);

        Assert.Contains("TheorySample", missing);
    }
}
