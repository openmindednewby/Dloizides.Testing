using Xunit;

namespace Dloizides.Testing.Tests.Samples.Unit.Battery;

[MethodUnderTest("Read", "Reads the stored battery level back.")]
public class BatteryReadSample
{
    [Fact]
    public void Read_WhenStored_ReturnsLevel()
    {
    }
}
