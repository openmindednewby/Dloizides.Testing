namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC01", "Reads every [Requirement] on a test class into the JSON requirements list with its text and source file path.")]
[MethodUnderTest("AC02", "Reads the ids of a [Covers] attribute into the covers list of that test's record.")]
[MethodUnderTest("AC03", "Takes the feature from [Feature] when present and falls back to TrxParser.FeatureOf when absent.")]
[MethodUnderTest("AC04", "Keeps one flow entry per [Flow] attribute when a method carries several.")]
[MethodUnderTest("AC18", "Reports a non-literal attribute argument with its file and line instead of silently dropping it.")]
public class AttributeReaderTests
{
    [Fact]
    public void AC01_WithTwoRequirementAttributes_ReportsBothIdsWithTextAndPath()
    {
        const string given = "a test class with [Requirement('AC-01','A')] and [Requirement('AC-02','B')]";
        const string when = "the report reads the source";
        const string then = "JSON requirements holds both ids with their text and the source file path";

        Assert.Fail($"AC-01 not implemented. Given {given}; when {when}; then {then}.");
    }

    [Fact]
    public void AC02_WithCoversTwoIds_RecordsBothInCovers()
    {
        const string given = "a method with [Covers('AC-01','AC-02')]";
        const string when = "the report reads the source";
        const string then = "the test record has covers ['AC-01','AC-02']";

        Assert.Fail($"AC-02 not implemented. Given {given}; when {when}; then {then}.");
    }

    [Fact]
    public void AC03_WithFeaturePresentOrAbsent_UsesItOrFallsBackToTrxParser()
    {
        const string given = "[Feature('X')] present / absent";
        const string when = "the report reads the source";
        const string then = "feature is X / falls back to TrxParser.FeatureOf";

        Assert.Fail($"AC-03 not implemented. Given {given}; when {when}; then {then}.");
    }

    [Fact]
    public void AC04_WithTwoFlowAttributesOnOneMethod_ReturnsTwoFlowEntries()
    {
        const string given = "[Flow('Submit', step: 2)] twice on one method";
        const string when = "the report reads the source";
        const string then = "two flow entries";

        Assert.Fail($"AC-04 not implemented. Given {given}; when {when}; then {then}.");
    }

    [Fact]
    public void AC18_WithNonLiteralAttributeArgument_ReportsItWithFileAndLine()
    {
        const string given = "[Covers(Ids.First)] or [Requirement(nameof(X), '...')]";
        const string when = "the reader scans";
        const string then = "it reports the non-literal argument with file:line, never silently drops it";

        Assert.Fail($"AC-18 not implemented. Given {given}; when {when}; then {then}.");
    }
}
