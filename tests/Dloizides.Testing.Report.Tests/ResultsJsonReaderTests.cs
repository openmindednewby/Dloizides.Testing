using System.Text.Json.Nodes;
using Shouldly;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Read", "Rejects every results document the committed schema file rejects, naming the offending field instead of crashing.")]
[MethodUnderTest("Parse", "Reports a requirement or covers id that breaks the shared id grammar as an attribute problem with its file and line.")]
public sealed class ResultsJsonReaderTests
{
    private const string Head = """{"schema":"testdoc-results.v1","run":{"name":"r"},""";

    [Theory]
    [InlineData(Head + """ "requirements":[],"tests":[{"id":"t","project":"p","set":"Unit","status":"pass"}]}""", "tests[0]")]
    [InlineData(Head + """ "requirements":[],"tests":[{"id":"t","framework":"xunit","set":"Unit","status":"pass"}]}""", "tests[0]")]
    [InlineData(Head + """ "requirements":[],"tests":[{"id":"t","framework":"xunit","project":"p","set":"Unit","status":"passed"}]}""", "tests[0].status")]
    [InlineData(Head + """ "requirements":[{"id":"AC-01","title":"A"}],"tests":[]}""", "requirements[0]")]
    [InlineData(Head + """ "requirements":[{"id":"1 bad","title":"A","source":"a.cs"}],"tests":[]}""", "requirements[0].id")]
    [InlineData(Head + """ "requirements":[],"tests":[{"id":"t","framework":"xunit","project":"p","set":"Unit","status":"pass","seconds":"slow"}]}""", "tests[0].seconds")]
    [InlineData(Head + """ "requirements":[],"tests":[{"id":"t","framework":"xunit","project":"p","set":"Unit","status":"pass","flows":[{"name":"Checkout","step":99999999999}]}]}""", "tests[0].flows[0].step")]
    [InlineData(Head + """ "requirements":[],"tests":[{"id":"t","framework":"trx","project":"p","set":"Unit","status":"pass"}]}""", "tests[0].framework")]
    public void Read_WithDocumentTheSchemaRejects_ThrowsNamingTheField(string json, string field)
    {
        var read = () => ResultsJsonReader.Read(json);

        read.ShouldThrow<ResultsJsonException>().Message.ShouldContain($"field \"{field}\"");
    }

    [Fact]
    public void Read_WithRepoShaAndStartTime_KeepsThemWhenWrittenBack()
    {
        const string json = """
            {"schema":"testdoc-results.v1","run":{"name":"r","repo":"shop","sha":"abc123","startedAt":"2026-10-08T10:24:00Z","finishedAt":"2026-10-08T10:24:05Z","seconds":5},
             "requirements":[],"tests":[{"id":"t","framework":"playwright","project":"p","set":"E2E","status":"pass"}]}
            """;

        var written = JsonNode.Parse(ResultsJsonWriter.Write(ResultsJsonReader.Read(json)))!;

        written.ShouldSatisfyAllConditions(
            () => written["run"]!["repo"]!.GetValue<string>().ShouldBe("shop"),
            () => written["run"]!["sha"]!.GetValue<string>().ShouldBe("abc123"),
            () => written["run"]!["startedAt"]!.GetValue<string>().ShouldBe("2026-10-08T10:24:00Z"),
            () => written["run"]!["finishedAt"]!.GetValue<string>().ShouldBe("2026-10-08T10:24:05Z"),
            () => written["tests"]![0]!["framework"]!.GetValue<string>().ShouldBe("playwright"));
    }

    [Fact]
    public void Parse_WithIdBreakingTheGrammar_ReportsAProblem()
    {
        const string source = """
            [Requirement("AC 01", "Spaces are not allowed")]
            public class SubmitTests
            {
                [Covers("9-starts-with-digit")]
                public void Submit_WhenValid_SendsIt() { }
            }
            """;

        var read = AttributeReader.Parse(source, "SubmitTests.cs");

        read.ShouldSatisfyAllConditions(
            () => read.Problems.Select(p => p.ToString()).ShouldBe([
                "SubmitTests.cs:1: [Requirement] id \"AC 01\" breaks the id grammar ^[A-Za-z][A-Za-z0-9_.-]*$, so the report cannot link it",
                "SubmitTests.cs:4: [Covers] id \"9-starts-with-digit\" breaks the id grammar ^[A-Za-z][A-Za-z0-9_.-]*$, so the report cannot link it"]),
            () => read.Requirements.ShouldBeEmpty(),
            () => read.Test("SubmitTests", "Submit_WhenValid_SendsIt").Covers.ShouldBeEmpty());
    }
}
