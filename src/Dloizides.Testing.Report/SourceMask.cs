namespace Dloizides.Testing.Report;

internal sealed class SourceMask
{
    private const char Hidden = ' ';
    private const char Backslash = (char)0x5C;
    private const char NewLine = (char)0x0A;
    private const char Apostrophe = (char)0x27;
    private const char Quote = '"';
    private const int RawQuotes = 3;

    private readonly string source;
    private readonly char[] masked;
    private int position;

    private SourceMask(string source)
    {
        this.source = source;
        masked = source.ToCharArray();
    }

    public static string Apply(string source)
    {
        var mask = new SourceMask(source);
        mask.Code(0);
        return new string(mask.masked);
    }

    private char At(int index) => index < source.Length ? source[index] : char.MinValue;

    private void Hide(int count)
    {
        for (var end = Math.Min(position + count, source.Length); position < end; position++)
            if (masked[position] != NewLine)
                masked[position] = Hidden;
    }

    private void Code(int closingBraces)
    {
        var depth = 0;
        while (position < source.Length)
        {
            var c = source[position];
            if (closingBraces > 0 && depth == 0 && c == '}')
            {
                Hide(closingBraces);
                return;
            }

            if (c == '/' && At(position + 1) == '/')
                LineComment();
            else if (c == '/' && At(position + 1) == '*')
                BlockComment();
            else if (c == Apostrophe)
                CharLiteral();
            else if (!TryString())
            {
                depth += c switch { '{' => 1, '}' => -1, _ => 0 };
                position++;
            }
        }
    }

    private void LineComment()
    {
        while (position < source.Length && source[position] != NewLine)
            Hide(1);
    }

    private void BlockComment()
    {
        Hide(2);
        while (position < source.Length && !(source[position] == '*' && At(position + 1) == '/'))
            Hide(1);
        Hide(2);
    }

    private void CharLiteral()
    {
        position++;
        while (position < source.Length && source[position] != Apostrophe && source[position] != NewLine)
            Hide(source[position] == Backslash ? 2 : 1);
        if (At(position) == Apostrophe)
            position++;
    }

    private bool TryString()
    {
        var start = position;
        var dollars = 0;
        var verbatim = false;
        for (; At(start) is '$' or '@'; start++)
            if (At(start) == '@')
                verbatim = true;
            else
                dollars++;

        if (At(start) != Quote)
            return false;

        position = start;
        var quotes = 0;
        while (At(position + quotes) == Quote)
            quotes++;
        if (!verbatim && quotes >= RawQuotes)
            Raw(quotes, dollars);
        else if (verbatim)
            Verbatim(dollars > 0);
        else
            Regular(dollars > 0);
        return true;
    }

    private void Regular(bool interpolated)
    {
        position++;
        while (position < source.Length && source[position] != NewLine)
        {
            var c = source[position];
            if (c == Quote)
            {
                position++;
                return;
            }

            if (c == Backslash)
                Hide(2);
            else if (interpolated && c == '{')
                Hole(1, raw: false);
            else
                Hide(1);
        }
    }

    private void Verbatim(bool interpolated)
    {
        position++;
        while (position < source.Length)
        {
            var c = source[position];
            var isEscapedQuote = c == Quote && At(position + 1) == Quote;
            if (isEscapedQuote)
                Hide(2);
            else if (c == Quote)
            {
                position++;
                return;
            }
            else if (interpolated && c == '{')
                Hole(1, raw: false);
            else
                Hide(1);
        }
    }

    private void Raw(int quotes, int dollars)
    {
        position += quotes;
        while (position < source.Length)
        {
            if (ClosesRaw(quotes))
            {
                position += quotes;
                return;
            }

            if (dollars > 0 && source[position] == '{')
                Hole(dollars, raw: true);
            else
                Hide(1);
        }
    }

    private bool ClosesRaw(int quotes)
    {
        for (var i = 0; i < quotes; i++)
            if (At(position + i) != Quote)
                return false;

        return true;
    }

    private void Hole(int braces, bool raw)
    {
        var run = 0;
        while (At(position + run) == '{')
            run++;
        var opens = raw ? run >= braces : run % 2 == 1;
        Hide(run);
        if (opens)
            Code(braces);
    }
}
