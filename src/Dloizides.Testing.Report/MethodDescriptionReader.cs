using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal static partial class MethodDescriptionReader
{
    private const string Literal = """
        "((?:[^"\\]|\\.)*)"
        """;

    private const string Declaration = @"\[MethodUnderTest\(\s*" + Literal + @"\s*,\s*((?:" + Literal + @"\s*\+?\s*)+)\)\]|\bclass\s+(\w+)";

    private const int ClassGroup = 4;

    public static Dictionary<string, string> ReadDirectories(IEnumerable<string> roots)
    {
        var descriptions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in SourceFiles(roots))
            Collect(File.ReadAllText(file), descriptions);

        return descriptions;
    }

    public static IEnumerable<string> SourceFiles(IEnumerable<string> roots) =>
        roots.Where(Directory.Exists).SelectMany(root => Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(Path.GetRelativePath(root, f)))
            .Order(StringComparer.Ordinal));

    public static Dictionary<string, string> Parse(string source)
    {
        var descriptions = new Dictionary<string, string>(StringComparer.Ordinal);
        Collect(source, descriptions);
        return descriptions;
    }

    public static string Key(string className, string method) => $"{className}|{method}";

    private static void Collect(string source, Dictionary<string, string> descriptions)
    {
        var pending = new List<(string Method, string Text)>();
        foreach (Match match in DeclarationPattern().Matches(source))
        {
            if (match.Groups[ClassGroup].Success)
            {
                foreach (var item in pending)
                    descriptions[Key(match.Groups[ClassGroup].Value, item.Method)] = item.Text;
                pending.Clear();
                continue;
            }

            var joined = string.Concat(LiteralPattern().Matches(match.Groups[2].Value).Select(m => m.Groups[1].Value));
            pending.Add((match.Groups[1].Value, EscapePattern().Replace(joined, "$1")));
        }
    }

    private static bool IsBuildOutput(string relativePath) =>
        relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(s => s is "bin" or "obj");

    [GeneratedRegex(Declaration)]
    private static partial Regex DeclarationPattern();

    [GeneratedRegex(Literal)]
    private static partial Regex LiteralPattern();

    [GeneratedRegex(@"\\(.)")]
    private static partial Regex EscapePattern();
}
