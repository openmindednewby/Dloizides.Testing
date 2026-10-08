using System.Text.Json.Nodes;
using Json.Schema;
using Shouldly;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Write", "Writes a trx run plus its source attributes as a testdoc-results.v1 document that the committed schema file accepts.")]
[MethodUnderTest("Schema", "The committed testdoc-results.v1 schema file rejects a document that breaks the contract, so a green Write check means something.")]
public sealed class ResultsJsonSchemaTests
{
    private const string Source = """
        [Requirement("AC-01", "Saved items load back")]
        public class AlphaTests
        {
            [Fact]
            [Covers("AC-01")]
            [Flow("Checkout", 2)]
            public void Load_WhenSaved_ReturnsIt() { }
        }
        """;

    private static readonly Lazy<JsonSchema> Contract = new(() =>
        JsonSchema.FromText(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "testdoc-results.v1.schema.json"))));

    [Fact]
    public void Write_WithTrxRunAndRequirement_MatchesTheSchemaFile()
    {
        var runFolder = Path.Combine(Path.GetTempPath(), "test-report-schema-" + Guid.NewGuid().ToString("N"), "20261007-100000");
        Directory.CreateDirectory(runFolder);
        File.WriteAllText(Path.Combine(runFolder, "Unit-Shop.Tests.trx"), TrxSample.Build(
            new SampleResult("Shop.Tests.Alpha.AlphaTests", "Shop.Tests.Alpha.AlphaTests.Load_WhenSaved_ReturnsIt", "Passed")));
        var options = new ReportOptions(runFolder, [], new HashSet<string>(), [], string.Empty);
        var run = new RunReader(options, new Dictionary<string, string>()).Read(runFolder);
        var attributes = AttributeReader.Parse(Source, "/src/Shop.Tests/AlphaTests.cs");

        var json = ResultsJsonWriter.Write(run, attributes, ["/src"]);

        var node = JsonNode.Parse(json)!;
        var result = Contract.Value.Evaluate(node, new EvaluationOptions { OutputFormat = OutputFormat.List });
        result.ShouldSatisfyAllConditions(
            () => result.IsValid.ShouldBeTrue(),
            () => node["requirements"]![0]!["source"]!.GetValue<string>().ShouldBe("Shop.Tests/AlphaTests.cs"),
            () => node["tests"]![0]!["covers"]![0]!.GetValue<string>().ShouldBe("AC-01"),
            () => node["tests"]![0]!["flows"]![0]!["step"]!.GetValue<int>().ShouldBe(2));
    }

    [Theory]
    [InlineData("""{"schema":"testdoc-results.v1","run":{"name":"r"},"requirements":[],"tests":[{"id":"t","framework":"xunit","project":"p","set":"Unit","status":"passed"}]}""")]
    [InlineData("""{"schema":"testdoc-results.v2","run":{"name":"r"},"requirements":[],"tests":[]}""")]
    [InlineData("""{"run":{"name":"r"},"requirements":[],"tests":[]}""")]
    [InlineData("""{"schema":"testdoc-results.v1","run":{"name":"r"},"requirements":[{"id":"AC-01","title":"A"}],"tests":[]}""")]
    public void Schema_WithBrokenContract_RejectsTheDocument(string json)
    {
        var node = JsonNode.Parse(json);

        var result = Contract.Value.Evaluate(node);

        result.IsValid.ShouldBeFalse();
    }
}
