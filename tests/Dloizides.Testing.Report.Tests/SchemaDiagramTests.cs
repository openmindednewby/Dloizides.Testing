using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC09", "Draws every table and foreign key from the EF model snapshot, coloured by the migration tests that cover it.")]
public class SchemaDiagramTests
{
    private const string Snapshot = """
        modelBuilder.Entity("Shop.A", b =>
            {
                b.Property<int>("Id");
                b.HasKey("Id");
                b.ToTable("a");
            });

        modelBuilder.Entity("Shop.B", b =>
            {
                b.Property<int>("Id");
                b.Property<int>("AId");
                b.HasKey("Id");
                b.ToTable("b");
            });

        modelBuilder.Entity("Shop.C", b =>
            {
                b.Property<int>("Id");
                b.HasKey("Id");
                b.ToTable("c");
            });

        modelBuilder.Entity("Shop.B", b =>
            {
                b.HasOne("Shop.A", "A")
                    .WithMany()
                    .HasForeignKey("AId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });
        """;

    [Fact]
    public void AC09_WithSnapshotAndMigrationResults_DrawsAllTablesColouredByCoverage()
    {
        TestResult[] tests =
        [
            Test("ATargetTests", "Columns", "WhenMigrated", TestStatus.Pass),
            Test("BTargetTests", "Columns", "WhenMigrated", TestStatus.Fail),
        ];

        var schema = SchemaDiagram.RenderSnapshot(Snapshot, tests);

        Assert.Equal(new Dictionary<string, string> { ["a"] = "pass", ["b"] = "fail", ["c"] = "plain" }, schema.TableClasses);
        Assert.Contains("a ||--o{ b : \"AId\"", schema.Mermaid, StringComparison.Ordinal);
        Assert.Contains("    c {\n", schema.Mermaid, StringComparison.Ordinal);
        Assert.Contains("class a pass\n    class b fail\n    class c plain", schema.Mermaid, StringComparison.Ordinal);
    }
}
