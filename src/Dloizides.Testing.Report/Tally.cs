namespace Dloizides.Testing.Report;

internal sealed class Tally
{
    private readonly int[] counts = new int[StatusText.CountOrder.Length];

    public Tally(IEnumerable<TestResult> tests)
    {
        foreach (var test in tests)
        {
            counts[(int)test.Status]++;
            Total++;
        }
    }

    public int Total { get; }

    public int this[TestStatus status] => counts[(int)status];

    public string Text()
    {
        var parts = StatusText.CountOrder.Where(s => this[s] > 0).Select(s => $"{this[s]} {StatusText.CountWord(s)}").ToList();
        return parts.Count == 0 ? "no tests" : string.Join(", ", parts);
    }
}

internal sealed record Verdict(string Tone, string Text, string Short)
{
    public const string Red = "red";
    public const string Amber = "amber";
    public const string Green = "green";

    public static Verdict For(TestSet set)
    {
        var tally = new Tally(set.Tests);
        var failed = tally[TestStatus.Fail];
        var early = tally[TestStatus.XPass];
        var expectedRed = tally[TestStatus.XFail];
        if (set.Missing.Count > 0)
            return new Verdict(Red, $"No results from {string.Join(", ", set.Missing)}. Read its .log.", "no results");
        if (tally.Total == 0)
            return new Verdict(Red, "No tests ran.", "no tests ran");
        if (failed > 0)
            return new Verdict(Red, $"{failed} failed unexpectedly.", $"{failed} failed");
        if (early > 0)
            return new Verdict(Red, $"{early} passed early. Move them out of the expected-red set.", $"{early} passed early");
        if (set.ExpectRed)
            return new Verdict(Amber, $"Red as expected: {expectedRed} targets are not built yet.", $"{expectedRed} expected red");
        var skipped = tally[TestStatus.Skip] > 0 ? $", {tally[TestStatus.Skip]} skipped" : string.Empty;
        return new Verdict(Green, $"All {tally[TestStatus.Pass]} passed{skipped}.", $"{tally[TestStatus.Pass]} passed");
    }
}
