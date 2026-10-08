using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Render", "Draws one diagram per area, escaping each label once and giving every area its own file name.")]
public class AreaDiagramsTests
{
    [Fact]
    public void Render_WithParticipantHoldingQuoteSemicolonAndHash_EscapesEachOnce()
    {
        var test = Test("BatteryTests", "Load", "WhenSaved", TestStatus.Pass) with
        {
            Calls = [new ResultsCall { Seq = 1, From = "Ops \"A\";#1", To = "HENEX", Method = "GET", Path = "/a;b", Status = 200 }],
        };

        var mermaid = SequenceDiagram.Render([test]).Single().Mermaid;

        Assert.Contains("participant p1 as Ops #quot;A#quot;#59;#35;1", mermaid, StringComparison.Ordinal);
        Assert.Contains("p1->>p2: GET /a#59;b", mermaid, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WithAreasSharingSlug_NamesFilesApart()
    {
        UseCaseEntry[] useCases = [new UseCaseEntry { Text = "Place order", Actor = "Trader" }];
        TestResult[] tests =
        [
            Test("OrderBookTests", "Place", "WhenOpen", TestStatus.Pass) with { Feature = "Order book", UseCases = useCases },
            Test("OrderBookTests", "Cancel", "WhenOpen", TestStatus.Pass) with { Feature = "order-book", UseCases = useCases },
        ];

        var files = UseCaseDiagram.Render(tests).Select(diagram => diagram.FileName).ToList();

        Assert.Equal(2, files.Distinct(StringComparer.Ordinal).Count());
    }
}
