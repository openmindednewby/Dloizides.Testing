namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC01", "Reads every [Requirement] on a test class into the JSON requirements list with its text and source file path.")]
[MethodUnderTest("AC02", "Reads the ids of a [Covers] attribute into the covers list of that test's record.")]
[MethodUnderTest("AC03", "Takes the feature from [Feature] when present and falls back to TrxParser.FeatureOf when absent.")]
[MethodUnderTest("AC04", "Keeps one flow entry per [Flow] attribute when a method carries several.")]
[MethodUnderTest("AC18", "Reports a non-literal attribute argument with its file and line instead of silently dropping it.")]
public class AttributeReaderTests
{
    private const string Path = "Declarations/SubmitTests.cs";

    [Fact]
    public void AC01_WithTwoRequirementAttributes_ReportsBothIdsWithTextAndPath()
    {
        const string source = """
            [Requirement("AC-01", "A")]
            [Requirement("AC-02", "B")]
            public class SubmitTests
            {
            }
            """;
        RequirementRecord[] expected = [new("AC-01", "A", Path, "SubmitTests"), new("AC-02", "B", Path, "SubmitTests")];

        var read = AttributeReader.Parse(source, Path);

        Assert.Equal(expected, read.Requirements);
    }

    [Fact]
    public void AC02_WithCoversTwoIds_RecordsBothInCovers()
    {
        const string source = """
            public class SubmitTests
            {
                [Fact]
                [Covers("AC-01", "AC-02")]
                public void Submit_WhenValid_SendsIt()
                {
                }
            }
            """;
        string[] expected = ["AC-01", "AC-02"];

        var read = AttributeReader.Parse(source, Path);

        Assert.Equal(expected, read.Test("SubmitTests", "Submit_WhenValid_SendsIt").Covers);
    }

    [Fact]
    public void AC03_WithFeaturePresentOrAbsent_UsesItOrFallsBackToTrxParser()
    {
        const string source = """
            [Feature("X")]
            public class TaggedTests
            {
                [Fact]
                public void Submit_WhenValid_SendsIt()
                {
                }
            }

            public class SubmitTests
            {
                [Fact]
                public void Submit_WhenValid_SendsIt()
                {
                }
            }
            """;
        const string method = "Submit_WhenValid_SendsIt";
        var fallback = TrxParser.FeatureOf("Trading.Tests.Declarations.SubmitTests", "Trading.Tests", "Unit");

        var read = AttributeReader.Parse(source, Path);

        Assert.Equal("X", read.FeatureOr("TaggedTests", method, fallback));
        Assert.Equal("Declarations", read.FeatureOr("SubmitTests", method, fallback));
    }

    [Fact]
    public void AC04_WithTwoFlowAttributesOnOneMethod_ReturnsTwoFlowEntries()
    {
        const string source = """
            public class SubmitTests
            {
                [Fact]
                [Flow("Submit", step: 2), Flow("Submit", step: 2)]
                public void Submit_WhenValid_SendsIt()
                {
                }
            }
            """;
        FlowEntry[] expected = [new("Submit", 2), new("Submit", 2)];

        var read = AttributeReader.Parse(source, Path);

        Assert.Equal(expected, read.Test("SubmitTests", "Submit_WhenValid_SendsIt").Flows);
    }

    [Fact]
    public void AC18_WithNonLiteralAttributeArgument_ReportsItWithFileAndLine()
    {
        const string source = """
            [Requirement(nameof(X), "Sends the declaration.")]
            public class SubmitTests
            {
                [Fact]
                [Covers(Ids.First)]
                public void Submit_WhenValid_SendsIt()
                {
                }
            }
            """;
        AttributeProblem[] expected = [new(Path, 1, "Requirement", "nameof(X)"), new(Path, 5, "Covers", "Ids.First")];

        var read = AttributeReader.Parse(source, Path);

        Assert.Equal(expected, read.Problems);
        Assert.Empty(read.Requirements);
    }
}
