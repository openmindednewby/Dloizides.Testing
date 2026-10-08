using System.Text;

namespace Dloizides.Testing.Report;

internal sealed record ReportPaths(string RunPage, string Latest);

internal static class ReportGenerator
{
    public const string LatestFile = "latest.html";

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public static ReportPaths Generate(ReportOptions options)
    {
        var reader = new RunReader(options, MethodDescriptionReader.ReadDirectories(options.SourceRoots));
        var runPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(options.RunFolder));
        var reports = Path.GetDirectoryName(runPath) ?? runPath;
        var run = options.ResultsFile is { } results ? FromJson(results, runPath) : FromTrx(reader, runPath, options);
        if (options.ResultsFile is null)
            File.WriteAllText(Path.Combine(runPath, ResultsJson.FileName), ResultsJsonWriter.Write(run), Utf8NoBom);

        var runIndex = Path.Combine(runPath, "index.html");
        var diagrams = RunDiagrams.Build(run, options.EfSnapshot is { } snapshot ? File.ReadAllText(snapshot) : null);
        diagrams.WriteFiles(runPath, Utf8NoBom);
        File.WriteAllText(runIndex, RunPage.Render(run, options.RunTitle, options.Labels, diagrams), Utf8NoBom);

        var runs = Directory.GetDirectories(reports)
            .Where(d => RunDates.Parse(Path.GetFileName(d)) is not null && File.Exists(Path.Combine(d, "index.html")))
            .Select(d => string.Equals(Path.GetFullPath(d), runPath, StringComparison.OrdinalIgnoreCase) ? run : reader.Read(d))
            .OrderByDescending(r => r.Date)
            .ToList();
        File.WriteAllText(Path.Combine(reports, "index.html"), IndexPage.Render(runs, options.IndexTitle, options.Labels), Utf8NoBom);
        var latest = Path.Combine(reports, LatestFile);
        File.WriteAllText(latest, IndexPage.Latest(runs.Count > 0 ? runs[0].Name : run.Name, options.RunTitle), Utf8NoBom);
        return new ReportPaths(runIndex, latest);
    }

    private static TestRun FromTrx(RunReader reader, string runPath, ReportOptions options) =>
        RecordedCalls.Attach(RunAttributes.Attach(reader.Read(runPath), AttributeReader.ReadDirectories(options.SourceRoots), options.SourceRoots), runPath);

    private static TestRun FromJson(string resultsFile, string runPath)
    {
        var name = Path.GetFileName(runPath);
        return ResultsJsonReader.Read(File.ReadAllText(resultsFile)) with { Name = name, Date = RunDates.Parse(name) };
    }
}

internal static class IndexPage
{
    private const string Head = "<table class=\"runs\"><thead><tr><th>Run</th><th>Sets</th><th class=\"num\">Took</th></tr></thead><tbody>";

    public static string Render(IReadOnlyList<TestRun> runs, string title, SetLabels labels)
    {
        var body = new StringBuilder($"<h1>{Html.Encode(title)}</h1>");
        var latestLink = runs.Count > 0 ? $" <a class=\"latest\" href=\"{ReportGenerator.LatestFile}\">Open the latest run</a>." : string.Empty;
        body.Append($"<p class=\"when\">Newest first. Amber means a set marked expected-red failed as expected.{latestLink}</p>");
        body.Append(Head);
        foreach (var run in runs)
            body.Append(Row(run, labels));
        body.Append("</tbody></table>");
        return Html.Page(title, body.ToString(), false);
    }

    public static string Latest(string newest, string runTitle)
    {
        var target = Html.Encode($"{newest}/index.html");
        return $"<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta http-equiv=\"refresh\" content=\"0; url={target}\">"
            + $"<title>Latest run: {Html.Encode(runTitle)}</title></head><body><a href=\"{target}\">{Html.Encode(newest)}</a></body></html>";
    }

    private static string Row(TestRun run, SetLabels labels)
    {
        var badges = string.Concat(run.Sets.Select(set =>
        {
            var verdict = Verdict.For(set);
            return $"<span class=\"badge {verdict.Tone}\"><b>{Html.Encode(set.Name)}</b> {Html.Encode(verdict.Short)}{labels.Span(set.Name, "blabel")}</span>";
        }));
        var setsHtml = badges.Length > 0 ? $"<div class=\"badges\">{badges}</div>" : "<span class=\"empty\">No .trx results in this folder</span>";
        var took = run.Seconds > 0 ? Html.Duration(run.Seconds) : string.Empty;
        var filter = run.SetFilter.Length > 0 ? $", set: {run.SetFilter}" : string.Empty;
        return $"<tr><td data-l=\"Run\"><a class=\"day\" href=\"{Html.Encode(run.Name)}/index.html\">{Html.Encode(Html.RunDate(run))}</a>"
            + $"<span class=\"sub\">{Html.Encode(run.Name + filter)}</span></td><td data-l=\"Sets\">{setsHtml}</td>"
            + $"<td class=\"num\" data-l=\"Took\">{took}</td></tr>";
    }
}
