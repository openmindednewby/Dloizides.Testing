using System.Text.Json;

namespace Dloizides.Testing.Report;

internal static class ResultsJsonWriter
{
    public static string Write(TestRun run, SourceAttributes attributes, IReadOnlyList<string> sourceRoots) =>
        Write(RunAttributes.Attach(run, attributes, sourceRoots));

    public static string Write(TestRun run)
    {
        var document = new ResultsDocument
        {
            Run = new ResultsRun
            {
                Name = run.Name,
                Repo = run.Repo,
                Sha = run.Sha,
                Seconds = run.Seconds,
                StartedAt = run.StartedAt,
                FinishedAt = run.FinishedAt,
                SetFilter = run.SetFilter,
                Sets = run.Sets.Select(ToSet).ToList(),
            },
            Requirements = run.Requirements,
            Features = run.Features,
            Tests = run.Sets.SelectMany(set => set.Tests.Select(test => ToTest(set, test))).ToList(),
        };
        return JsonSerializer.Serialize(document, ResultsJson.Options);
    }

    private static ResultsSet ToSet(TestSet set) => new()
    {
        Name = set.Name,
        ExpectRed = set.ExpectRed,
        Files = set.Files.Select(f => new ResultsFile { Project = f.Project, Trx = f.Trx, Log = f.Log }).ToList(),
        Missing = [.. set.Missing],
    };

    private static ResultsTest ToTest(TestSet set, TestResult test) => new()
    {
        Id = test.Name,
        Framework = test.Framework,
        Project = test.Project,
        Set = set.Name,
        ExpectRed = set.ExpectRed,
        Feature = test.Feature,
        Class = test.Class,
        Method = test.Method,
        Description = test.Description,
        Scenario = test.Scenario,
        Expected = test.Expected,
        Args = test.Args,
        Covers = test.Covers,
        Flows = test.Flows.Select(f => new ResultsFlow { Name = f.Name, Step = f.Step }).ToList(),
        Status = ResultsJson.StatusName(test.Status),
        Seconds = test.Seconds,
        Message = test.Message,
        Stack = test.Stack,
        Calls = test.Calls,
        UseCases = test.UseCases,
    };
}
