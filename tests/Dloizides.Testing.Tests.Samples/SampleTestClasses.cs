using Xunit;

namespace Dloizides.Testing.Tests.Samples;

public sealed class LiveFactAttribute : FactAttribute;

[MethodUnderTest("Load", "Loads the persisted value back by its id.")]
public class DescribedSample
{
    [Fact]
    public void Load_WhenStored_ReturnsValue()
    {
    }
}

public class UndescribedSample
{
    [Fact]
    public void Save_WhenNew_Persists()
    {
    }
}

public class TheorySample
{
    [Theory]
    public void Parse_WithInput_ReturnsNumber(int input)
    {
        _ = input;
    }
}

public class CustomFactSample
{
    [LiveFact]
    public void Fetch_WhenRemoteUp_ReturnsData()
    {
    }
}

public class NameOnlyFactSample
{
    [Foreign.Fact]
    public void Count_WhenEmpty_ReturnsZero()
    {
    }
}

[MethodUnderTest("Delete", "Deletes the row and returns its id.")]
public class StaleDescriptionSample
{
    [Fact]
    public void Archive_WhenOld_MovesRow()
    {
    }
}

[MethodUnderTest("Merge", "Merges.")]
public class ShortDescriptionSample
{
    [Fact]
    public void Merge_WhenTwoRows_KeepsNewest()
    {
    }
}

public abstract class AbstractSample
{
    [Fact]
    public void Reset_WhenCalled_ClearsState()
    {
    }
}

[MethodUnderTest("Export", "No.")]
public class NoTestsSample
{
    public void Export_WhenCalled_WritesFile()
    {
    }
}

[MethodUnderTest("Sum", "Adds every line of the invoice together.")]
[MethodUnderTest("Round", "Rounds the invoice total to whole cents.")]
public class TwoDescriptionsSample
{
    [Fact]
    public void Sum_WhenLines_AddsThem()
    {
    }

    [Fact]
    public void Round_WhenFraction_RoundsHalfUp()
    {
    }
}

[MethodUnderTest("Reset", "Clears every cached value so the next read goes to the store.")]
public abstract class DescribedBaseSample
{
    [Fact]
    public void Reset_WhenCalled_ClearsCache()
    {
    }
}

public class InheritedDescriptionSample : DescribedBaseSample;

public static class StaticFactSample
{
    [Fact]
    public static void Compute_WhenCalled_ReturnsTotal()
    {
    }
}

[MethodUnderTest("Submit", "Submits the declaration to the market operator.")]
[Requirement("AC-01", "A declared requirement that no test covers.")]
[Requirement("AC-02", "A declared requirement that one test covers.")]
public class RequirementSample
{
    [Fact]
    [Covers("AC-02")]
    public void Submit_WhenValid_SendsIt()
    {
    }

    [Fact]
    [Covers("AC-99")]
    public void Submit_WhenLate_RejectsIt()
    {
    }
}

[MethodUnderTest("Submit", "Submits the declaration to the market operator.")]
[Requirement("AC-02", "The same id RequirementSample covers, never covered in this class.")]
public class ScopedRequirementSample
{
    [Fact]
    public void Submit_WhenValid_SendsIt()
    {
    }
}

[Requirement("AC-03", "A declared requirement on a class with no tests.")]
public class RequirementWithoutTestsSample;

[MethodUnderTest("Submit", "Submits the declaration to the market operator.")]
public class ForeignCoversSample
{
    [Fact]
    [Covers("AC-01")]
    public void Submit_WhenForeign_ReportsIt()
    {
    }
}

[Requirement("AC-04", "A requirement declared on an abstract base and covered by its derived class.")]
public abstract class AbstractRequirementSample;

[MethodUnderTest("Submit", "Submits the declaration to the market operator.")]
public class DerivedRequirementSample : AbstractRequirementSample
{
    [Fact]
    [Covers("AC-04")]
    public void Submit_WhenDerived_CoversTheBase()
    {
    }
}
