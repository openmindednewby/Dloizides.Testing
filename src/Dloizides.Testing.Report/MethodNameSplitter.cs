using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal static partial class MethodNameSplitter
{
    public static string ToWords(string identifier)
    {
        if (identifier.Length == 0)
            return string.Empty;
        var words = WordBoundary().Replace(identifier, " ").Split(' ');
        for (var i = 1; i < words.Length; i++)
        {
            if (CapitalWord().IsMatch(words[i]))
                words[i] = words[i].ToLowerInvariant();
        }

        return string.Join(' ', words);
    }

    public static MethodNameParts Split(string method)
    {
        var parts = method.Split('_');
        if (parts.Length == 1)
            return new MethodNameParts(method, string.Empty, string.Empty);
        if (parts.Length == 2)
            return new MethodNameParts(parts[0], string.Empty, ToWords(parts[1]));
        var middle = parts[1..^1].Select(ToWords).ToArray();
        for (var i = 1; i < middle.Length; i++)
        {
            if (CapitalLead().IsMatch(middle[i]))
                middle[i] = char.ToLowerInvariant(middle[i][0]) + middle[i][1..];
        }

        return new MethodNameParts(parts[0], string.Join(", ", middle), ToWords(parts[^1]));
    }

    [GeneratedRegex("(?<=[a-z])(?=[A-Z0-9])|(?<=[0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex WordBoundary();

    [GeneratedRegex("^[A-Z][a-z]+$")]
    private static partial Regex CapitalWord();

    [GeneratedRegex("^[A-Z][a-z]+( |$)")]
    private static partial Regex CapitalLead();
}
