using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Render", "Puts each method of a flow step on its own line so Mermaid never splits a name mid-word.")]
public class FlowLabelTests
{
    [Fact]
    public void Render_WithTwoMethodsAtOneStep_PutsEachOnItsOwnLine()
    {
        TestResult[] tests =
        [
            Test("AdmieDataFetchJobTests", "FetchAllAsync", "WhenDue", TestStatus.Pass) with { Flows = [new FlowEntry("Import", 1)] },
            Test("AdmieRepositoryTests", "InsertFileEntry", "WhenNew", TestStatus.Pass) with { Flows = [new FlowEntry("Import", 1)] },
        ];

        var flow = Assert.Single(FlowDiagram.Render(tests));

        Assert.Contains("s1[\"Step 1: AdmieDataFetchJob.FetchAllAsync<br/>AdmieRepository.InsertFileEntry\"]:::pass", flow.Mermaid, StringComparison.Ordinal);
    }
}
