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
