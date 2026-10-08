using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Render", "Draws the snapshot's tables as distinct ER entities, each covered by its <Table>TargetTests class.")]
public class SchemaCoverageTests
{
    [Fact]
    public void Render_WithTableNamesThatSanitiseAlike_KeepsTwoEntities()
    {
        var model = new SchemaModel(["a-b", "a_b"], [new SchemaRelation("a-b", "a_b", "AbId")]);

        var schema = SchemaDiagram.Render(model, []);

        Assert.Contains("    a_b {\n", schema.Mermaid, StringComparison.Ordinal);
        Assert.Contains("    a_b_2 {\n", schema.Mermaid, StringComparison.Ordinal);
        Assert.Contains("a_b ||--o{ a_b_2 : \"AbId\"", schema.Mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithMethodNamedAfterTableButOtherClass_LeavesTablePlain()
    {
        var model = new SchemaModel(["Parks", "ParksCommercial"], []);
        TestResult[] tests =
        [
            Test("ParksRepositoryTests", "ParksCommercialColumns", "WhenRead", TestStatus.Fail),
            Test("ParksCommercialTargetTests", "Columns", "WhenMerged", TestStatus.XFail),
        ];

        var schema = SchemaDiagram.Render(model, tests);

        Assert.Equal(new Dictionary<string, string> { ["Parks"] = "plain", ["ParksCommercial"] = "xfail" }, schema.TableClasses);
    }
}
