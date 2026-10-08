namespace Dloizides.Testing.Report;

internal static class FeatureTitle
{
    private const string TestsSuffix = "Tests";

    public static ResultsFeature? Find(IReadOnlyList<ResultsFeature> features, string area)
    {
        var key = FeatureArea.Key(area);
        return features.FirstOrDefault(feature => FeatureArea.Key(feature.Name) == key);
    }

    public static string Words(string name) => name.Any(char.IsWhiteSpace) ? name : Join(Split(name));

    public static string Class(string className)
    {
        var words = Split(Subject(className));
        return Join(words.Count > 1 ? words.Take(words.Count - 1).ToList() : words);
    }

    public static string Kind(string className) => Split(Subject(className)).Count > 1 ? Badges.TypeOf(className) : string.Empty;

    public static string Bare(string className) => className[(className.LastIndexOf('+') + 1)..];

    public static string Subject(string className)
    {
        var bare = Bare(className);
        return bare.Length > TestsSuffix.Length && bare.EndsWith(TestsSuffix, StringComparison.Ordinal) ? bare[..^TestsSuffix.Length] : bare;
    }

    private static List<string> Split(string name)
    {
        var words = new List<string>();
        var start = 0;
        for (var i = 1; i < name.Length; i++)
        {
            if (!IsWordStart(name, i))
                continue;
            words.Add(name[start..i]);
            start = i;
        }

        if (name.Length > start)
            words.Add(name[start..]);
        return words;
    }

    private static bool IsWordStart(string name, int i)
    {
        var afterLower = !char.IsUpper(name[i - 1]);
        var endsAcronym = i + 1 < name.Length && char.IsLower(name[i + 1]);
        return char.IsUpper(name[i]) && (afterLower || endsAcronym);
    }

    private static string Join(IReadOnlyList<string> words) =>
        string.Join(" ", words.Select((word, i) => i == 0 || IsAcronym(word) ? word : word.ToLowerInvariant()));

    private static bool IsAcronym(string word) => word.Length > 1 && word.All(char.IsUpper);
}
