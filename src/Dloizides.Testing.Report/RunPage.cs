using System.Text;

namespace Dloizides.Testing.Report;

internal sealed class RunPage
{
    private const int SearchMessageLength = 300;
    private const int SummaryMessageLength = 240;
    private const string SearchBox =
        "<div class=\"search\"><input id=\"q\" type=\"search\" placeholder=\"Filter by test, feature or error text\" aria-label=\"Filter tests\"><output id=\"qn\" for=\"q\"></output></div>";
    private const string TableHead =
        "<thead><tr><th>Scenario</th><th>Expected</th><th>Outcome</th><th class=\"num\">Time</th></tr></thead>";

    private static readonly StringComparer Ordering = StringComparer.OrdinalIgnoreCase;

    private readonly List<(string Id, string Name)> problems = [];
    private int testCounter;
    private int groupCounter;

    public static string Render(TestRun run, string title) => new RunPage().Build(run, title);

    private static string E(string text) => Html.Encode(text);

    private string Build(TestRun run, string title)
    {
        var body = new StringBuilder("<a class=\"back\" href=\"../index.html\">All test runs</a>");
        body.Append($"<h1>{E(title)}</h1>");
        var took = run.Seconds > 0 ? $", took {Html.Duration(run.Seconds)}" : string.Empty;
        body.Append($"<p class=\"when\">{E(Html.RunDate(run))}{took}</p>");
        body.Append(Headline(run)).Append(SetList(run));
        var sections = string.Concat(run.Sets.Where(s => s.Tests.Count > 0).Select(Section));
        if (problems.Count > 0)
            body.Append("<ol class=\"attn\">").Append(string.Concat(problems.Select(p => $"<li><a href=\"#{p.Id}\">{E(p.Name)}</a></li>"))).Append("</ol>");
        if (testCounter > 0)
            body.Append(SearchBox);
        body.Append(sections);
        return Html.Page($"{title} {Html.RunDate(run)}", body.ToString(), testCounter > 0);
    }

    private static string Headline(TestRun run)
    {
        if (run.Sets.Count == 0)
            return "<p class=\"headline red\">This run has no .trx results.</p>";
        var tones = run.Sets.Select(s => (s.Name, Verdict.For(s).Tone)).ToList();
        var red = tones.Where(t => t.Tone == Verdict.Red).Select(t => t.Name).ToList();
        if (red.Count > 0)
            return $"<p class=\"headline red\">Needs a look: {E(string.Join(", ", red))} did not end as expected.</p>";
        var amber = tones.Where(t => t.Tone == Verdict.Amber).Select(t => t.Name).ToList();
        var amberText = amber.Count > 0 ? $" {string.Join(", ", amber)} is red as expected." : string.Empty;
        return $"<p class=\"headline\">Nothing failed unexpectedly.{E(amberText)}</p>";
    }

    private static string SetList(TestRun run)
    {
        var builder = new StringBuilder("<ul class=\"sets\">");
        foreach (var set in run.Sets)
        {
            var verdict = Verdict.For(set);
            var tally = new Tally(set.Tests);
            var links = string.Join("<br>", set.Files.Select(FileLinks));
            builder.Append($"<li class=\"set {verdict.Tone}\"><span class=\"set-name\">{E(set.Name)}</span><span class=\"set-verdict\">{E(verdict.Text)}</span>")
                .Append($"{Html.TallyBar(tally, string.Empty)}<span class=\"counts\">{E(tally.Text())}</span><span class=\"files\">{links}</span></li>");
        }

        return builder.Append("</ul>").ToString();
    }

    private static string FileLinks(SetFile file)
    {
        var log = file.Log.Length > 0 ? $" <a href=\"{E(file.Log)}\">log</a>" : string.Empty;
        return $"{E(file.Project)}: <a href=\"{E(file.Trx)}\">trx</a>{log}";
    }

    private string Section(TestSet set)
    {
        var builder = new StringBuilder($"<section class=\"setd\"><h2>{E(set.Name)}<small>{E(new Tally(set.Tests).Text())}</small></h2>");
        var multiProject = set.Files.Select(f => f.Project).Distinct(StringComparer.Ordinal).Count() > 1;
        var features = set.Tests
            .GroupBy(t => $"{t.Project}|{t.Feature}")
            .OrderBy(g => g.Min(t => (int)t.Status))
            .ThenBy(g => g.Key, Ordering);
        foreach (var feature in features)
            builder.Append(Feature(feature.ToList(), multiProject));
        return builder.Append("</section>").ToString();
    }

    private string Feature(List<TestResult> tests, bool multiProject)
    {
        var first = tests[0];
        var tally = new Tally(tests);
        var open = tally[TestStatus.Fail] + tally[TestStatus.XPass] > 0 ? " open" : string.Empty;
        var projectTag = multiProject ? $"<span class=\"proj\">{E(first.Project)}</span>" : string.Empty;
        var rows = MethodRows(tests);
        return $"<details class=\"feat\"{open}><summary><span class=\"fname\">{E(first.Feature)}</span>{projectTag}"
            + $"<span class=\"fcount\">{E(tally.Text())}</span>{Html.TallyBar(tally, "small")}</summary>"
            + $"<table>{TableHead}{rows}</table></details>";
    }

    private string MethodRows(List<TestResult> tests)
    {
        var builder = new StringBuilder();
        var methods = tests
            .GroupBy(t => (t.Class, t.Method))
            .OrderBy(g => g.Min(t => (int)t.Status))
            .ThenBy(g => g.Key.Class, Ordering)
            .ThenBy(g => g.Key.Method, Ordering);
        foreach (var method in methods)
        {
            groupCounter++;
            var group = $"g-{groupCounter}";
            var first = method.First();
            var description = first.Description.Length > 0
                ? $"<span class=\"desc\">{E(first.Description)}</span>"
                : "<span class=\"desc empty\">No description yet.</span>";
            builder.Append($"<tbody class=\"mh\" data-g=\"{group}\"><tr><th colspan=\"4\" scope=\"rowgroup\"><span class=\"m\">{E(first.Method)}</span>")
                .Append($"<span class=\"cls\">{E(first.Class)}</span>{description}</th></tr></tbody>");
            foreach (var test in method.OrderBy(t => (int)t.Status).ThenBy(t => t.Name, Ordering))
                builder.Append(TestRow(test, group));
        }

        return builder.ToString();
    }

    private string TestRow(TestResult test, string group)
    {
        testCounter++;
        var id = $"t-{testCounter}";
        if (test.Status is TestStatus.Fail or TestStatus.XPass)
            problems.Add((id, test.Name));
        var message = test.Message.Length > SearchMessageLength ? test.Message[..SearchMessageLength] : test.Message;
        var search = E($"{test.Name} {test.Feature} {test.Description} {StatusText.Label(test.Status)} {message}".ToLowerInvariant());
        var args = test.Args.Length > 0 ? $"<code class=\"args\">{E(test.Args)}</code>" : string.Empty;
        var scenario = test.Scenario.Length > 0 ? E(test.Scenario) : "<span class=\"empty\">any input</span>";
        var css = StatusText.Css(test.Status);
        return $"<tbody class=\"t st-{css}\" id=\"{id}\" data-g=\"{group}\" data-s=\"{search}\"><tr>"
            + $"<td data-l=\"Scenario\" title=\"{E(test.Name)}\">{scenario}{args}</td><td data-l=\"Expected\">{E(test.Expected)}</td>"
            + $"<td data-l=\"Outcome\"><span class=\"pill {css}\"><span class=\"sw {css}\"></span>{StatusText.Label(test.Status)}</span></td>"
            + $"<td class=\"num\" data-l=\"Time\">{Html.Duration(test.Seconds)}</td></tr>{Note(test)}</tbody>";
    }

    private static string Note(TestResult test)
    {
        if (test.Status == TestStatus.Skip)
        {
            var reason = test.Message.Length > 0 ? test.Message : "no reason given";
            return $"<tr class=\"note\"><td colspan=\"4\"><span class=\"why\">Skipped:</span> {E(reason)}</td></tr>";
        }

        if (test.Message.Length == 0 && test.Stack.Length == 0)
            return string.Empty;
        var first = test.Message.Split('\n')[0];
        if (first.Length > SummaryMessageLength)
            first = first[..SummaryMessageLength] + "...";
        var full = string.Join("\n\n", new[] { test.Message, test.Stack }.Where(p => p.Length > 0));
        return $"<tr class=\"note {StatusText.Css(test.Status)}\"><td colspan=\"4\"><details><summary><span class=\"msg\">{E(first)}</span></summary>"
            + $"<pre>{E(full)}</pre></details></td></tr>";
    }
}
