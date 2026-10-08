namespace Dloizides.Testing.Report;

internal sealed class RunRows
{
    private const string NotBuiltYet = "Not built yet";
    private const int SummaryMessageLength = 240;

    private readonly List<(string Id, string Name)> problems = [];

    public IReadOnlyList<(string Id, string Name)> Problems => problems;

    public int Count { get; private set; }

    private static string E(string text) => Html.Encode(text);

    public string Row(TestResult test)
    {
        Count++;
        var id = $"t-{Count}";
        if (test.Status is TestStatus.Fail or TestStatus.XPass)
            problems.Add((id, test.Name));
        var search = E(SearchText.Build(test));
        var args = test.Args.Length > 0 ? $"<code class=\"args\">{E(test.Args)}</code>" : string.Empty;
        var scenario = test.Scenario.Length > 0 ? E(test.Scenario) : "<span class=\"empty\">any input</span>";
        var css = StatusText.Css(test.Status);
        return $"<tbody class=\"t st-{css}\" id=\"{id}\" data-s=\"{search}\"><tr>"
            + $"<td data-l=\"Scenario\" title=\"{E(test.Name)}\">{scenario}{args}</td><td data-l=\"Expected\">{E(test.Expected)}</td>"
            + $"<td class=\"r\"><span class=\"pill {css}\"><span class=\"sw {css}\"></span>{StatusText.Label(test.Status)}</span></td>"
            + $"<td class=\"num\" data-l=\"Time\">{Html.Duration(test.Seconds)}</td></tr>{Note(test)}</tbody>";
    }

    private static string Note(TestResult test)
    {
        if (test.Status == TestStatus.Skip)
        {
            var reason = test.Message.Length > 0 ? test.Message : "no reason given";
            return $"<tr class=\"why skip\"><td colspan=\"4\" data-k=\"Skipped because\">{E(reason)}</td></tr>";
        }

        if (test.Message.Length == 0 && test.Stack.Length == 0)
            return string.Empty;
        var first = test.Status == TestStatus.XFail ? NotBuiltYet : test.Message.Split('\n')[0];
        if (first.Length > SummaryMessageLength)
            first = first[..SummaryMessageLength] + "...";
        var full = string.Join("\n\n", new[] { test.Message, test.Stack }.Where(p => p.Length > 0));
        return $"<tr class=\"note {StatusText.Css(test.Status)}\"><td colspan=\"4\"><details><summary><span class=\"msg\">{E(first)}</span></summary>"
            + $"<pre>{E(full)}</pre></details></td></tr>";
    }
}

internal static class Badges
{
    private static readonly (string Suffix, string Label)[] Types =
    [
        ("EndpointTests", "Endpoint"), ("ControllerTests", "Controller"), ("JobTests", "Job"), ("RepositoryTests", "Repository"),
        ("ServiceTests", "Service"), ("ParserTests", "Parser"), ("TargetTests", "Schema target"), ("HandlerTests", "Handler"),
        ("ValidatorTests", "Validator"), ("ConsumerTests", "Consumer"), ("ClientTests", "Client"), ("MapperTests", "Mapper"),
        ("ReaderTests", "Reader"), ("WriterTests", "Writer"), ("GuardTests", "Guard"),
    ];

    private static readonly TestStatus[] ChipOrder = [TestStatus.Pass, TestStatus.Fail, TestStatus.Skip, TestStatus.XFail];

    public static string TypeOf(string className)
    {
        var bare = className[(className.LastIndexOf('+') + 1)..];
        return Types.FirstOrDefault(t => bare.EndsWith(t.Suffix, StringComparison.Ordinal)).Label ?? "Test";
    }

    public static string Count(int scenarios) => scenarios == 1 ? "1 scenario" : $"{scenarios} scenarios";

    public static string Dot(Tally tally)
    {
        var red = tally[TestStatus.Fail] + tally[TestStatus.XPass];
        if (red == 0 && tally[TestStatus.Skip] == 0)
            return string.Empty;
        var (tone, text) = red > 0 ? ("red", $"{red} failed") : ("amber", $"{tally[TestStatus.Skip]} skipped");
        return $"<span class=\"dot {tone}\" title=\"Needs attention: {text}\"></span><span class=\"sr\">Needs attention: {text}</span>";
    }

    public static string Chips(Tally tally)
    {
        var chips = ChipOrder.Select(s =>
        {
            var count = s == TestStatus.Fail ? tally[TestStatus.Fail] + tally[TestStatus.XPass] : tally[s];
            var css = StatusText.Css(s);
            return $"<span class=\"chip {css}{(count == 0 ? " zero" : string.Empty)}\"><span class=\"sw\"></span>{count} {StatusText.CountWord(s)}</span>";
        });
        return $"<span class=\"chips\">{string.Concat(chips)}</span>";
    }
}
