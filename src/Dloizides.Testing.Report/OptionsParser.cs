namespace Dloizides.Testing.Report;

internal sealed record ReportOptions(
    string RunFolder,
    IReadOnlyList<string> SourceRoots,
    IReadOnlySet<string> ExpectedRedSets,
    IReadOnlyList<string> SetOrder,
    string Name)
{
    public string RunTitle => Name.Length > 0 ? $"{Name} tests" : "Tests";

    public string IndexTitle => Name.Length > 0 ? $"{Name} test runs" : "Test runs";
}

internal sealed record ParseResult(ReportOptions? Options, string Error, bool IsHelp);

internal static class OptionsParser
{
    public const string Usage =
        "Usage: test-report <run-folder> [--source <dir>]... [--expected-red <set>[,<set>...]]... [--set-order <set>[,<set>...]] [--name <product>]";

    public static ParseResult Parse(IReadOnlyList<string> args)
    {
        string? runFolder = null;
        var sources = new List<string>();
        var expectedRed = new HashSet<string>(StringComparer.Ordinal);
        var setOrder = new List<string>();
        var name = string.Empty;
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            if (arg is "-h" or "--help")
                return new ParseResult(null, string.Empty, true);
            if (!arg.StartsWith('-'))
            {
                if (runFolder is not null)
                    return Fail($"Unexpected argument \"{arg}\".");
                runFolder = arg;
                continue;
            }

            if (i + 1 >= args.Count)
                return Fail($"{arg} needs a value.");
            var value = args[++i];
            switch (arg)
            {
                case "--source": sources.Add(Path.GetFullPath(value)); break;
                case "--expected-red": expectedRed.UnionWith(SplitList(value)); break;
                case "--set-order": setOrder.AddRange(SplitList(value)); break;
                case "--name": name = value.Trim(); break;
                default: return Fail($"Unknown option \"{arg}\".");
            }
        }

        if (runFolder is null)
            return Fail("Missing <run-folder>.");
        var options = new ReportOptions(Path.GetFullPath(runFolder), sources, expectedRed, setOrder, name);
        return new ParseResult(options, string.Empty, false);
    }

    private static ParseResult Fail(string error) => new(null, error, false);

    private static string[] SplitList(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
