namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC09", "Draws every table and foreign key from the EF model snapshot, coloured by the migration tests that cover it.")]
public class SchemaDiagramTests
{
    [Fact]
    public void AC09_WithSnapshotAndMigrationResults_DrawsAllTablesColouredByCoverage()
    {
        const string given = "an EF model snapshot fixture with tables a, b, c and an FK b->a; migration tests cover a (pass) and b (fail)";
        const string when = "the ER diagram is rendered";
        const string then = "all three tables and the FK drawn from the snapshot; a green, b red, c plain";

        Assert.Fail($"AC-09 not implemented. Given {given}; when {when}; then {then}.");
    }
}
