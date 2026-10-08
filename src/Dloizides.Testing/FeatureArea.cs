namespace Dloizides.Testing;

internal static class FeatureArea
{
    private const string TestsSuffix = "Tests";

    public static string Of(string className, string prefix, string setName)
    {
        var rest = className.StartsWith(prefix + ".", StringComparison.Ordinal) ? className[(prefix.Length + 1)..] : className;
        var segments = rest.Split('.');
        if (segments.Length > 1 && segments[0] == setName)
            segments = segments[1..];
        if (segments.Length > 1)
            return segments[0];
        var head = segments[0].Split('+')[0];
        return head.Length > TestsSuffix.Length && head.EndsWith(TestsSuffix, StringComparison.Ordinal) ? head[..^TestsSuffix.Length] : head;
    }

    public static string Key(string name) => new(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
