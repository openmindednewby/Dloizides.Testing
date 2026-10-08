using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("AC08", "Draws one node per flow step in step order, grey for a step whose only test was skipped.")]
public class FlowDiagramTests
{
    [Fact]
    public void AC08_WithStepTwoOnlySkipped_DrawsThreeOrderedNodesWithStepTwoGrey()
    {
        TestResult[] tests =
        [
            Test("RepositoryTests", "Store", "WhenNew", TestStatus.Pass) with { Flows = [new FlowEntry("Import", 3)] },
            Test("ParserTests", "Parse", "WhenValid", TestStatus.Skip) with { Flows = [new FlowEntry("Import", 2)] },
            Test("JobTests", "Fetch", "WhenDue", TestStatus.Pass) with { Flows = [new FlowEntry("Import", 1)] },
        ];

        var flows = FlowDiagram.Render(tests);

        var flow = Assert.Single(flows);
        Assert.Equal([(1, "pass"), (2, "skip"), (3, "pass")], flow.Steps.Select(s => (s.Step, s.Class)));
        Assert.Contains("s2[\"Step 2: Parser.Parse\"]:::skip", flow.Mermaid, StringComparison.Ordinal);
        Assert.True(flow.Mermaid.IndexOf("s1[", StringComparison.Ordinal) < flow.Mermaid.IndexOf("s2[", StringComparison.Ordinal));
        Assert.True(flow.Mermaid.IndexOf("s2[", StringComparison.Ordinal) < flow.Mermaid.IndexOf("s3[", StringComparison.Ordinal));
        Assert.Contains("s1 --> s2\n    s2 --> s3", flow.Mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void AC08_WithFlowNamesSharingASlug_GivesEachFlowItsOwnAnchorAndFile()
    {
        TestResult[] tests =
        [
            Test("JobTests", "Fetch", "WhenDue", TestStatus.Pass) with { Flows = [new FlowEntry("import-batch", 1)] },
            Test("ParserTests", "Parse", "WhenValid", TestStatus.Pass) with { Flows = [new FlowEntry("Import Batch", 1)] },
        ];

        var flows = FlowDiagram.Render(tests);

        Assert.Equal(["f-1-import-batch", "f-2-import-batch"], flows.Select(f => f.Anchor));
        Assert.Equal(["flow-1-import-batch.mmd", "flow-2-import-batch.mmd"], flows.Select(f => f.FileName));
    }
}
