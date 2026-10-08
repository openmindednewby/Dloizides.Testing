using System.Text;
using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal static partial class MermaidText
{
    private const string TestsSuffix = "Tests";

    [GeneratedRegex("[^A-Za-z0-9_]")]
    private static partial Regex NotIdentifier();

    public static string Label(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            builder.Append(c switch
            {
                '#' => "#35;",
                '"' => "#quot;",
                '<' => "#lt;",
                '>' => "#gt;",
                '`' => "#96;",
                ';' => "#59;",
                '\r' or '\n' or '\t' => " ",
                _ => c.ToString(),
            });
        }

        return builder.ToString();
    }

    public static string Entity(string name)
    {
        var safe = NotIdentifier().Replace(name, "_");
        return safe.Length == 0 || char.IsDigit(safe[0]) ? "t_" + safe : safe;
    }

    public static string Slug(string name)
    {
        var slug = NotIdentifier().Replace(name, "-").Trim('-').ToLowerInvariant();
        return slug.Length == 0 ? "flow" : slug;
    }

    public static string FileStem(string name) => $"{Slug(name)}-{Fnv(name):x8}";

    public static string ShortClass(string className)
    {
        var bare = className[(className.LastIndexOf('+') + 1)..];
        return bare.Length > TestsSuffix.Length && bare.EndsWith(TestsSuffix, StringComparison.Ordinal) ? bare[..^TestsSuffix.Length] : bare;
    }

    public static string MethodKey(TestResult test) => $"{ShortClass(test.Class)}.{test.Method}";

    private static uint Fnv(string text)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var hash = offset;
        foreach (var c in text)
            hash = (hash ^ c) * prime;
        return hash;
    }
}
