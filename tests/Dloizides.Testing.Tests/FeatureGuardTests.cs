using System.Reflection;
using Dloizides.Testing.Report;
using Dloizides.Testing.Tests.Samples;
using Dloizides.Testing.Tests.Samples.Unit.Battery;

namespace Dloizides.Testing.Tests;

[MethodUnderTest("FD4", "Lists every area that has tests but no assembly-level [Feature] with a Why, so consumers can assert the list is empty.")]
public class FeatureGuardTests
{
    private static readonly Assembly Samples = typeof(DescribedSample).Assembly;

    [Fact]
    public void FD4_WithAreaWithoutFeature_ListsIt()
    {
        const string area = nameof(UndescribedSample);

        var missing = FeatureGuard.MissingFeatures(Samples);

        Assert.Contains(area, missing);
    }

    [Fact]
    public void FD4_WithAreaDeclaredByAssemblyFeature_DoesNotListIt()
    {
        const string area = nameof(DescribedSample);

        var missing = FeatureGuard.MissingFeatures(Samples);

        Assert.DoesNotContain(area, missing);
    }

    [Fact]
    public void FD4_WithClassFeatureNeverDeclared_ListsItsName()
    {
        var area = typeof(CheckoutFeatureSample).GetCustomAttribute<FeatureAttribute>()!.Name;

        var missing = FeatureGuard.MissingFeatures(Samples);

        Assert.Contains(area, missing);
    }

    [Fact]
    public void FD4_WithDeclaredFeatureWithoutWhy_ListsIt()
    {
        const string area = nameof(TheorySample);

        var missing = FeatureGuard.MissingFeatures(Samples);

        Assert.Contains(area, missing);
    }

    [Fact]
    public void FD4_WithSetNamespace_NamesTheAreaLikeTheReport()
    {
        const string set = "Unit";
        var reportArea = TrxParser.FeatureOf(typeof(BatteryReadSample).FullName!, Samples.GetName().Name!, set);

        var missing = FeatureGuard.MissingFeatures(Samples, set);

        Assert.Contains(reportArea, missing);
        Assert.DoesNotContain(set, missing);
    }
}
