using System.Text;

namespace Dloizides.Testing.Report;

internal sealed class RunPage
{
    private const string TestsSuffix = "Tests";

    private readonly SetLabels labels;
    private readonly RunDiagrams diagrams;

    private RunPage(SetLabels labels, RunDiagrams diagrams)
    {
        this.labels = labels;
        this.diagrams = diagrams;
    }

    public static string Render(TestRun run, string title, SetLabels labels) => Render(run, title, labels, RunDiagrams.None);

    public static string Render(TestRun run, string title, SetLabels labels, RunDiagrams diagrams) => new RunPage(labels, diagrams).Build(run, title);

    private static string E(string text) => Html.Encode(text);

    private string Build(TestRun run, string title)
    {
        var tests = run.Sets.SelectMany(s => s.Tests).ToList();
        var multiProject = tests.Select(t => t.Project).Distinct(StringComparer.Ordinal).Count() > 1;
        var tree = new RunTree(diagrams.Flows);
        var treeHtml = tree.Render(tests, multiProject);
        var body = new StringBuilder("<a class=\"back\" href=\"../index.html\">All test runs</a>");
        body.Append($"<h1>{E(title)}</h1>");
        var took = run.Seconds > 0 ? $", took {Html.Duration(run.Seconds)}" : string.Empty;
        body.Append($"<p class=\"when\">{E(Html.RunDate(run))}{took}</p>");
        body.Append(Headline(run, tests)).Append(SetList(run));
        if (tree.Problems.Count > 0)
            body.Append("<ol class=\"attn\">").Append(string.Concat(tree.Problems.Select(p => $"<li><a href=\"#{p.Id}\">{E(p.Name)}</a></li>"))).Append("</ol>");
        body.Append(RunSections.Diagrams(diagrams, RunTree.ThingId));
        if (tree.TestCount > 0)
        {
            body.Append(RunSections.Toolbar)
                .Append("<div class=\"shell\"><nav class=\"areas\" aria-label=\"Business areas\"><h2>Areas</h2><ul>")
                .Append(tree.Nav).Append("</ul></nav><main id=\"tree\">").Append(treeHtml).Append("</main></div>");
        }

        var tail = diagrams.Any ? RunDiagrams.MermaidScript : string.Empty;
        return Html.Page($"{title} {Html.RunDate(run)}", body.ToString(), tree.TestCount > 0, tail);
    }

    private static string Headline(TestRun run, IReadOnlyList<TestResult> tests)
    {
        if (run.Sets.Count == 0)
            return "<p class=\"headline red\">This run has no .trx results.</p>";
        var tally = new Tally(tests);
        var tones = run.Sets.Select(s => (s.Name, Verdict.For(s).Tone)).ToList();
        var red = tones.Where(t => t.Tone == Verdict.Red).Select(t => t.Name).ToList();
        string text;
        if (red.Count > 0)
            text = $"Needs a look: {string.Join(", ", red)} did not end as expected.";
        else
        {
            var amber = tones.Where(t => t.Tone == Verdict.Amber).Select(t => t.Name).ToList();
            text = "Nothing failed unexpectedly." + (amber.Count > 0 ? $" {string.Join(", ", amber)} is red as expected." : string.Empty);
        }

        var css = red.Count > 0 ? "headline red" : "headline";
        return $"<div class=\"{css}\"><span>{E(text)} {tally.Total} tests: {E(tally.Text())}.</span>{Html.TallyBar(tally, string.Empty)}</div>";
    }

    private string SetList(TestRun run)
    {
        var builder = new StringBuilder("<ul class=\"sets\">");
        foreach (var set in run.Sets)
        {
            var verdict = Verdict.For(set);
            var tally = new Tally(set.Tests);
            var links = string.Join("<br>", set.Files.Select(FileLinks));
            builder.Append($"<li class=\"set {verdict.Tone}\"><span class=\"set-name\">{E(set.Name)}</span><span class=\"set-verdict\">{E(verdict.Text)}</span>{labels.Span(set.Name, "set-label")}")
                .Append($"{Html.TallyBar(tally, string.Empty)}<span class=\"counts\">{E(tally.Text())}</span><span class=\"files\">{links}</span></li>");
        }

        return builder.Append("</ul>").ToString();
    }

    private static string FileLinks(SetFile file)
    {
        var log = file.Log.Length > 0 ? $" <a href=\"{E(file.Log)}\">run log</a>" : string.Empty;
        return $"{E(ProjectLabel(file.Project))}: <a href=\"{E(file.Trx)}\">raw results</a>{log}";
    }

    private static string ProjectLabel(string project) =>
        project.Length > TestsSuffix.Length && project.EndsWith(TestsSuffix, StringComparison.Ordinal)
            ? project[..^TestsSuffix.Length].TrimEnd('.')
            : project;
}
