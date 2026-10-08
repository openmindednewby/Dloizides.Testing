namespace Dloizides.Testing.Report;

internal static class RunAttributes
{
    public static TestRun Attach(TestRun run, SourceAttributes attributes, IReadOnlyList<string> sourceRoots) => run with
    {
        Sets = run.Sets.Select(set => AttachSet(set, attributes)).ToList(),
        Requirements = attributes.Requirements.Select(r => ToRequirement(r, sourceRoots)).ToList(),
    };

    private static TestSet AttachSet(TestSet set, SourceAttributes attributes)
    {
        var attached = new TestSet(set.Name, set.ExpectRed);
        attached.Files.AddRange(set.Files);
        attached.Missing.AddRange(set.Missing);
        attached.Tests.AddRange(set.Tests.Select(test => AttachTest(test, attributes)));
        return attached;
    }

    private static TestResult AttachTest(TestResult test, SourceAttributes attributes)
    {
        var declared = attributes.Test(AfterLast(test.Class, '+'), SourceMethod(test.Name));
        return test with { Covers = declared.Covers, Flows = declared.Flows, Feature = declared.Feature ?? test.Feature };
    }

    private static ResultsRequirement ToRequirement(RequirementRecord requirement, IReadOnlyList<string> sourceRoots) => new()
    {
        Id = requirement.Id,
        Title = requirement.Text,
        Source = RelativeSource(requirement.Path, sourceRoots),
    };

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
