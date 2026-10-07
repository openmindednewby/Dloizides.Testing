using Shouldly;

namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("For", "Returns the plain sentence shown beside a set name.")]
[MethodUnderTest("Parse", "Reads repeatable --set-label Name=Sentence options.")]
public sealed class SetLabelsTests
{
    [Fact]
    public void For_WithBaselineAndNoOverride_ReturnsDefaultSentence()
    {
        SetLabels.None.For("baseline").ShouldBe("Today's behaviour — must pass");
    }

    [Fact]
    public void For_WithUnknownSet_ReturnsEmpty()
    {
        SetLabels.None.For("Whatever").ShouldBe(string.Empty);
    }

    [Fact]
    public void For_WithOverride_ReturnsTheOverride()
    {
        var labels = new SetLabels(new Dictionary<string, string> { ["Baseline"] = "Must pass" });

        labels.For("Baseline").ShouldBe("Must pass");
    }

    [Fact]
    public void Parse_WithRepeatedSetLabel_KeepsEachSentenceIncludingEquals()
    {
        string[] args = ["runs/20261007-100000", "--set-label", "Live=Real calls", "--set-label", "Slow=a=b"];

        var labels = OptionsParser.Parse(args).Options!.Labels;

        (labels.For("Live"), labels.For("Slow")).ShouldBe(("Real calls", "a=b"));
    }

    [Fact]
    public void Parse_WithoutEquals_ReturnsError()
    {
        string[] args = ["runs/20261007-100000", "--set-label", "Live"];

        OptionsParser.Parse(args).Error.ShouldBe("--set-label needs Name=Sentence, got \"Live\".");
    }
}
