using static Dloizides.Testing.Report.Tests.DiagramSamples;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("TB1", "Shows a short search placeholder that fits the box while the hidden label keeps the full wording.")]
[MethodUnderTest("TB2", "Keeps a gap between an area or class title and its kind tag.")]
[MethodUnderTest("TB3", "Keeps the toolbar stuck to the top on a phone, with the controls two to a row.")]
[MethodUnderTest("TB4", "Lets every toolbar control fit one row at desktop width by capping the selects and search boxes.")]
[MethodUnderTest("TB5", "Keeps a gap between each legend mark and its words.")]
public class ToolbarLayoutTests
{
    private const StringComparison Ordinal = StringComparison.Ordinal;

    [Fact]
    public void TB1_WithSearchBox_ShowsShortPlaceholderAndFullLabel()
    {
        var set = new TestSet("Unit", false);
        set.Tests.Add(Test("SubmitTests", "Submit", "WhenValid", TestStatus.Pass));
        var run = new TestRun("20261007-100000", null, [set], 0, string.Empty);

        var html = RunPage.Render(run, "Shop tests", SetLabels.None, RunDiagrams.Build(run, null));

        Assert.Contains("<label class=\"sr\" for=\"q\">Search tests or tasks</label><input id=\"q\" type=\"search\" placeholder=\"Search tests\" autocomplete=\"off\">", html, Ordinal);
    }

    [Fact]
    public void TB2_WithKindTag_KeepsGapAfterTitle()
    {
        Assert.Contains(".gname .kind{margin-left:8px}", Assets.Css, Ordinal);
    }

    [Fact]
    public void TB3_WhenPhoneWidth_KeepsToolbarSticky()
    {
        const string twoUp = "@media (max-width:720px){.tools{padding:6px 0}.tools>select,.tools>.combo,.tools>button{flex:1 1 calc(50% - 6px);max-width:none}#q{flex:1 1 100%}}";

        Assert.DoesNotContain(".tools{position:static}", Assets.Css, Ordinal);
        Assert.Contains(twoUp, Assets.Css, Ordinal);
    }

    [Fact]
    public void TB4_WhenDesktopWidth_CapsControlsToFitOneRow()
    {
        Assert.Contains(".tools input{flex:1 1 10rem;", Assets.Css, Ordinal);
        Assert.Contains(".combo{position:relative;flex:1 1 10rem;", Assets.Css, Ordinal);
        Assert.Contains(".tools select{min-height:44px;min-width:0;max-width:13rem;", Assets.Css, Ordinal);
    }

    [Fact]
    public void TB5_WithLegend_KeepsGapAfterMark()
    {
        Assert.Contains(".legend>span{display:inline-flex;align-items:center;gap:6px}", Assets.Css, Ordinal);
    }
}
