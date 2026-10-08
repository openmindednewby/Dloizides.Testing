using Xunit;

[assembly: Dloizides.Testing.Feature("Described sample", Why = "Loads persisted values back by id.", Context = "Sample only.", Owner = "Testing")]
[assembly: Dloizides.Testing.Feature("Theory sample")]

namespace Dloizides.Testing.Tests.Samples;

[Feature("Checkout")]
[MethodUnderTest("Pay", "Charges the order total once when the payment is due.")]
public class CheckoutFeatureSample
{
    [Fact]
    public void Pay_WhenDue_Charges()
    {
    }
}
