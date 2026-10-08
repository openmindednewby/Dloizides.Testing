using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("MermaidScript", "Loads the pinned Mermaid build from cdnjs only when its sha512 integrity hash matches.")]
[MethodUnderTest("WriteFiles", "Writes each diagram as a .mmd file and a standalone full-size page beside the run page.")]
[MethodUnderTest("Build", "Builds the run's diagrams; the schema is coloured only by target tests in expected-red sets.")]
public class RunDiagramsTests
{
    [Fact]
    public void MermaidScript_WithPinnedVersion_CarriesSubresourceIntegrity()
    {
        const string integrity = "integrity=\"sha512-3Ix7UjkWptQ1zS6VvZzzy4QPkYfhcr5fFOmxbsvgT/83J563DkKHEVHV0jmFNtSyfRU9SLfLsGTly/QpkzegHQ==\"";

        var script = RunDiagrams.MermaidScript;

        Assert.Contains("src=\"https://cdnjs.cloudflare.com/ajax/libs/mermaid/11.6.0/mermaid.min.js\" " + integrity, script, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WithTargetTestOutsideExpectedRedSet_LeavesItsTablePlain()
    {
        const string snapshot = """
            modelBuilder.Entity("Shop.A", b => { b.ToTable("a"); });
            modelBuilder.Entity("Shop.B", b => { b.ToTable("b"); });
            """;
        var baseline = new TestSet("Baseline", false);
        baseline.Tests.Add(Test("ATargetTests", "Columns", "WhenMigrated", TestStatus.Fail));
        var target = new TestSet("MigrationTarget", true);
        target.Tests.Add(Test("BTargetTests", "Columns", "WhenMigrated", TestStatus.XFail));
        var run = new TestRun("20261007-100000", null, [baseline, target], 0, string.Empty);

        var diagrams = RunDiagrams.Build(run, snapshot);

        Assert.Equal(new Dictionary<string, string> { ["a"] = "plain", ["b"] = "xfail" }, diagrams.Schema!.TableClasses);
    }

    [Fact]
    public void MermaidScript_WithDarkSystemTheme_RendersAtNaturalSizeInDarkTheme()
    {
        const string darkQuery = "matchMedia(\"(prefers-color-scheme: dark)\").matches";

        var script = RunDiagrams.MermaidScript;

        Assert.Contains(darkQuery, script, StringComparison.Ordinal);
        Assert.Contains("theme:dark?\"dark\":\"neutral\"", script, StringComparison.Ordinal);
        Assert.Contains("er:{useMaxWidth:false}", script, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteFiles_WithSchema_WritesFullSizePageBesideTheMmd()
    {
        var folder = Path.Combine(Path.GetTempPath(), "test-report-mmd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var diagrams = new RunDiagrams(null, [], new SchemaResult("erDiagram\n    a {\n    }\n", new Dictionary<string, string>()));

        diagrams.WriteFiles(folder, new System.Text.UTF8Encoding(false));

        var page = File.ReadAllText(Path.Combine(folder, "schema.html"));
        Directory.Delete(folder, true);
        Assert.Contains("<div class=\"mermaid\">erDiagram\n    a {\n    }\n</div>", page, StringComparison.Ordinal);
        Assert.Contains(RunDiagrams.MermaidScript, page, StringComparison.Ordinal);
    }
}
