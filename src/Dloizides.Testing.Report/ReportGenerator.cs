using System.Text;

namespace Dloizides.Testing.Report;

internal static class ReportGenerator
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public static string Generate(ReportOptions options)
    {
        var reader = new RunReader(options, MethodDescriptionReader.ReadDirectories(options.SourceRoots));
        var runPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.RunFolder));
        var reports = Path.GetDirectoryName(runPath) ?? runPath;
        var run = reader.Read(runPath);
        var runIndex = Path.Combine(runPath, "index.html");
        File.WriteAllText(runIndex, RunPage.Render(run, options.RunTitle), Utf8NoBom);

        var runs = Directory.GetDirectories(reports)
            .Where(d => RunDates.Parse(Path.GetFileName(d)) is not null)
            .Select(d => string.Equals(Path.GetFullPath(d), runPath, StringComparison.OrdinalIgnoreCase) ? run : reader.Read(d))
            .OrderByDescending(r => r.Date)
            .ToList();
        File.WriteAllText(Path.Combine(reports, "index.html"), IndexPage.Render(runs, options.IndexTitle), Utf8NoBom);
        if (runs.Count > 0)
            File.WriteAllText(Path.Combine(reports, "latest.html"), IndexPage.Latest(runs[0].Name, options.RunTitle), Utf8NoBom);
        return runIndex;
    }
}

internal static class IndexPage
{
    private const string Head = "<table class=\"runs\"><thead><tr><th>Run</th><th>Sets</th><th class=\"num\">Took</th></tr></thead><tbody>";

    public static string Render(IReadOnlyList<TestRun> runs, string title)
    {
        var body = new StringBuilder($"<h1>{Html.Encode(title)}</h1>");
        var latestLink = runs.Count > 0 ? " <a href=\"latest.html\">Open the latest run</a>." : string.Empty;
        body.Append($"<p class=\"when\">Newest first. Amber means a set marked expected-red failed as expected.{latestLink}</p>");
        body.Append(Head);
        foreach (var run in runs)
            body.Append(Row(run));
        body.Append("</tbody></table>");
        return Html.Page(title, body.ToString(), false);
    }

    public static string Latest(string newest, string runTitle)
    {
        var target = Html.Encode($"{newest}/index.html");
        return $"<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta http-equiv=\"refresh\" content=\"0; url={target}\">"
            + $"<title>Latest run: {Html.Encode(runTitle)}</title></head><body><a href=\"{target}\">{Html.Encode(newest)}</a></body></html>";
    }

    private static string Row(TestRun run)
    {
        var badges = string.Concat(run.Sets.Select(set =>
        {
            var verdict = Verdict.For(set);
            return $"<span class=\"badge {verdict.Tone}\"><b>{Html.Encode(set.Name)}</b> {Html.Encode(verdict.Short)}</span>";
        }));
        var setsHtml = badges.Length > 0 ? $"<div class=\"badges\">{badges}</div>" : "<span class=\"empty\">No .trx results in this folder</span>";
        var took = run.Seconds > 0 ? Html.Duration(run.Seconds) : string.Empty;
        var filter = run.SetFilter.Length > 0 ? $", set: {run.SetFilter}" : string.Empty;
        return $"<tr><td data-l=\"Run\"><a class=\"day\" href=\"{Html.Encode(run.Name)}/index.html\">{Html.Encode(Html.RunDate(run))}</a>"
            + $"<span class=\"sub\">{Html.Encode(run.Name + filter)}</span></td><td data-l=\"Sets\">{setsHtml}</td>"
            + $"<td class=\"num\" data-l=\"Took\">{took}</td></tr>";
    }
}
