using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal sealed record Argument(string Raw, string? Name, int Position, string? Text, int? Number);

internal static partial class ArgumentSplitter
{
    public static IReadOnlyList<Argument> Split(string arguments) =>
        TopLevelParts(arguments)
            .Select(part => part.Trim())
            .Where(part => part.Length > 0)
            .Select(Read)
            .ToList();

    private static Argument Read(string part, int position)
    {
        var named = NamedPattern().Match(part);
        var name = named.Success ? named.Groups[1].Value : null;
        var value = named.Success ? part[named.Length..].Trim() : part;
        var literal = LiteralPattern().Match(value);
        var text = literal.Success ? EscapePattern().Replace(literal.Groups[1].Value, "$1") : null;
        int? number = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
        return new Argument(value, name, position, text, number);
    }

    private static IEnumerable<string> TopLevelParts(string arguments)
    {
        var current = new StringBuilder();
        var depth = 0;
        var inString = false;
        for (var i = 0; i < arguments.Length; i++)
        {
            var c = arguments[i];
            var isSeparator = c == ',' && depth == 0 && !inString;
            if (isSeparator)
            {
                yield return current.ToString();
                current.Clear();
                continue;
            }

            current.Append(c);
            if (inString && c == '\\' && i + 1 < arguments.Length)
                current.Append(arguments[++i]);
            else if (c == '"')
                inString = !inString;
            else if (!inString)
                depth += c switch { '(' => 1, ')' => -1, _ => 0 };
        }

        yield return current.ToString();
    }

    [GeneratedRegex(@"^(\w+)\s*:(?!:)")]
    private static partial Regex NamedPattern();

    [GeneratedRegex("""^"((?:[^"\\]|\\.)*)"$""")]
    private static partial Regex LiteralPattern();

    [GeneratedRegex(@"\\(.)")]
    private static partial Regex EscapePattern();
}
