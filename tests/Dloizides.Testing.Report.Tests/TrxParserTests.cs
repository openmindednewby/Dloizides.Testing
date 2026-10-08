using Shouldly;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Parse", "Reads every result of a .trx file into a report row with its status, timing, error and description.")]
[MethodUnderTest("FeatureOf", "Names the feature a test class belongs to from its namespace below the project, or its class name.")]
public class TrxParserTests
{
    private const string Project = "Shop.Tests";
    private const string ClassName = "Shop.Tests.Orders.OrderServiceTests";
    private static readonly Dictionary<string, string> NoDescriptions = [];

    [Theory]
    [InlineData("executor://xunit/VsTestRunner2/netcoreapp", "xunit")]
    [InlineData("executor://NUnit3TestExecutor", "nunit")]
    [InlineData("executor://MSTestAdapter/v2", "mstest")]
    [InlineData("", "xunit")]
    public void Parse_WithAdapterTypeName_ReadsTheFramework(string adapter, string expected)
    {
        var xml = TrxSample.Build(new SampleResult(ClassName, ClassName + ".Save_WhenNew_ReturnsId", "Passed", Adapter: adapter));

        var test = TrxParser.Parse(xml, new TrxContext("Unit", Project, false), NoDescriptions).Tests.Single();

        test.Framework.ShouldBe(expected);
    }

    [Fact]
    public void Parse_WithDefinitionAndDescription_ReadsClassMethodAndDescription()
    {
        var xml = TrxSample.Build(new SampleResult(ClassName, ClassName + ".Save_WhenNew_ReturnsId", "Passed"));
        var descriptions = new Dictionary<string, string> { ["OrderServiceTests|Save"] = "Stores a new order." };

        var test = TrxParser.Parse(xml, new TrxContext("Unit", Project, false), descriptions).Tests.Single();

        test.ShouldSatisfyAllConditions(
            () => test.Feature.ShouldBe("Orders"),
            () => test.Class.ShouldBe("OrderServiceTests"),
            () => test.Method.ShouldBe("Save"),
            () => test.Description.ShouldBe("Stores a new order."),
            () => test.Scenario.ShouldBe("When new"),
            () => test.Expected.ShouldBe("Returns id"),
            () => test.Status.ShouldBe(TestStatus.Pass),
            () => test.Seconds.ShouldBe(1.5));
    }

    [Fact]
    public void Parse_WithTheoryArguments_SplitsArgumentsFromName()
    {
        var xml = TrxSample.Build(new SampleResult(ClassName, ClassName + ".Save_WithCount_Stores(count: 2)", "Passed"));

        var test = TrxParser.Parse(xml, new TrxContext("Unit", Project, false), NoDescriptions).Tests.Single();

        test.Args.ShouldBe("count: 2");
    }

    [Theory]
    [InlineData("Passed", false, "Pass")]
    [InlineData("Failed", false, "Fail")]
    [InlineData("Failed", true, "XFail")]
    [InlineData("Passed", true, "XPass")]
    [InlineData("NotExecuted", true, "Skip")]
    public void Parse_WithOutcomeAndExpectation_MapsToStatus(string outcome, bool expectRed, string expected)
    {
        var xml = TrxSample.Build(new SampleResult(ClassName, ClassName + ".Save_WhenNew_ReturnsId", outcome));

        var test = TrxParser.Parse(xml, new TrxContext("Unit", Project, expectRed), NoDescriptions).Tests.Single();

        test.Status.ToString().ShouldBe(expected);
    }

    [Fact]
    public void Parse_WithErrorInfo_ReadsMessageAndStack()
    {
        const string output = "<Output><ErrorInfo><Message>  Expected 2 but was 3  </Message><StackTrace>at Save()\n</StackTrace></ErrorInfo></Output>";
        var xml = TrxSample.Build(new SampleResult(ClassName, ClassName + ".Save_WhenNew_ReturnsId", "Failed", output));

        var test = TrxParser.Parse(xml, new TrxContext("Unit", Project, false), NoDescriptions).Tests.Single();

        (test.Message, test.Stack).ShouldBe(("Expected 2 but was 3", "at Save()"));
    }

    [Fact]
    public void Parse_WithTimes_ReadsStartAndFinish()
    {
        var xml = TrxSample.Build();

        var file = TrxParser.Parse(xml, new TrxContext("Unit", Project, false), NoDescriptions);

        (file.Start, file.Finish).ShouldBe((DateTimeOffset.Parse(TrxSample.Start), DateTimeOffset.Parse(TrxSample.Finish)));
    }

    [Fact]
    public void FeatureOf_WithSetNamespace_SkipsTheSetSegment()
    {
        const string className = "Shop.Tests.Unit.Billing.InvoiceTests";

        var feature = TrxParser.FeatureOf(className, Project, "Unit");

        feature.ShouldBe("Billing");
    }

    [Fact]
    public void FeatureOf_WithClassDirectlyUnderProject_StripsTestsSuffix()
    {
        const string className = "Shop.Tests.InvoiceTests";

        var feature = TrxParser.FeatureOf(className, Project, "Unit");

        feature.ShouldBe("Invoice");
    }

    [Fact]
    public void FeatureOf_WithNestedClass_UsesTheOuterClass()
    {
        const string className = "Shop.Tests.InvoiceTests+WhenPaid";

        var feature = TrxParser.FeatureOf(className, Project, "Unit");

        feature.ShouldBe("Invoice");
    }
}
