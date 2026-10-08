using System.Text.Json;

namespace Dloizides.Testing.Report;

internal static class ResultsJsonWriter
{
    public static string Write(TestRun run, SourceAttributes attributes, IReadOnlyList<string> sourceRoots)
    {
        var document = new ResultsDocument
        {
            Run = new ResultsRun
            {
                Name = run.Name,
                Seconds = run.Seconds,
                SetFilter = run.SetFilter,
                Sets = run.Sets.Select(ToSet).ToList(),
            },
            Requirements = attributes.Requirements.Select(r => ToRequirement(r, sourceRoots)).ToList(),
            Tests = run.Sets.SelectMany(set => set.Tests.Select(test => ToTest(set, test, attributes))).ToList(),
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

    private static ResultsRequirement ToRequirement(RequirementRecord requirement, IReadOnlyList<string> sourceRoots) => new()
    {
        Id = requirement.Id,
        Title = requirement.Text,
        Source = RelativeSource(requirement.Path, sourceRoots),
    };

    private static ResultsTest ToTest(TestSet set, TestResult test, SourceAttributes attributes)
    {
        var declared = attributes.Test(AfterLast(test.Class, '+'), SourceMethod(test.Name));
        return new ResultsTest
        {
            Id = test.Name,
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
            Covers = declared.Covers,
            Flows = declared.Flows.Select(f => new ResultsFlow { Name = f.Name, Step = f.Step }).ToList(),
            Status = ResultsJson.StatusName(test.Status),
            Seconds = test.Seconds,
            Message = test.Message,
            Stack = test.Stack,
        };
    }

    private static string SourceMethod(string testName)
    {
        var paren = testName.IndexOf('(', StringComparison.Ordinal);
        return AfterLast(paren >= 0 ? testName[..paren] : testName, '.');
    }

    private static string RelativeSource(string path, IReadOnlyList<string> sourceRoots)
    {
        var full = Path.GetFullPath(path);
        var root = sourceRoots.Select(Path.GetFullPath)
            .FirstOrDefault(r => full.StartsWith(Path.TrimEndingDirectorySeparator(r) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
        var relative = root is null ? path : Path.GetRelativePath(root, full);
        return relative.Replace('\\', '/');
    }

    private static string AfterLast(string text, char separator) => text[(text.LastIndexOf(separator) + 1)..];
}
