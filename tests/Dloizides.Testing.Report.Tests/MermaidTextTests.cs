namespace Dloizides.Testing.Report.Tests;

[MethodUnderTest("Label", "Turns any text into a Mermaid label that cannot end the quoted string, start an entity or inject markup.")]
public class MermaidTextTests
{
    [Fact]
    public void Label_WithHostileCharacters_EncodesEachOne()
    {
        const string text = "a\"b#c<d>e`f\ng";

        var label = MermaidText.Label(text);

        Assert.Equal("a#quot;b#35;c#lt;d#gt;e#96;f g", label);
    }
}
