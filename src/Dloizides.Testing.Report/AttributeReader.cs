using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal static partial class AttributeReader
{
    private const string Arguments = """
        (?<args>(?:"(?:[^"\\]|\\.)*"|[^()"]|\([^()]*\))*)
        """;

    private const string Token = @"(?<=[\[,]\s*)(?<attr>Requirement|Covers|Feature|Flow)(?:Attribute)?\s*\(" + Arguments +
        @"\)|\bclass\s+(?<class>\w+)|\b(?:void|Task|ValueTask)\s+(?<method>\w+)\s*\(";

    private const string StepName = "step";
    private const int StepPosition = 1;

    public static SourceAttributes ReadDirectories(IEnumerable<string> roots)
    {
        var read = new SourceAttributes();
        foreach (var file in MethodDescriptionReader.SourceFiles(roots))
            Collect(File.ReadAllText(file), file, read);

        return read;
    }

    public static SourceAttributes Parse(string source, string path)
    {
        var read = new SourceAttributes();
        Collect(source, path, read);
        return read;
    }

    private static void Collect(string source, string path, SourceAttributes read)
    {
        var pending = new List<Match>();
        var currentClass = string.Empty;
        foreach (Match match in TokenPattern().Matches(source))
        {
            if (match.Groups["attr"].Success)
            {
                pending.Add(match);
                continue;
            }

            var isClass = match.Groups["class"].Success;
            if (isClass)
                currentClass = match.Groups["class"].Value;
            var key = isClass ? currentClass : MethodDescriptionReader.Key(currentClass, match.Groups["method"].Value);
            var target = Target(isClass ? read.Classes : read.Methods, key);
            foreach (var attribute in pending)
                Apply(new Declaration(source, path, attribute, currentClass), read, target);
            pending.Clear();
        }
    }

    private static AttributeSet Target(Dictionary<string, AttributeSet> sets, string key)
    {
        if (!sets.TryGetValue(key, out var set))
            sets[key] = set = new AttributeSet();
        return set;
    }

    private static void Apply(Declaration declaration, SourceAttributes read, AttributeSet target)
    {
        var arguments = ArgumentSplitter.Split(declaration.Match.Groups["args"].Value);
        var problems = arguments.Where(argument => !IsExpected(declaration.Name, argument)).ToList();
        read.Problems.AddRange(problems.Select(argument => declaration.Problem(argument.Raw)));
        if (declaration.Name == "Covers")
        {
            target.Covers.AddRange(arguments.Except(problems).Select(argument => argument.Text!));
            return;
        }

        if (problems.Count == 0)
            ApplyComplete(declaration, arguments, read, target);
    }

    private static void ApplyComplete(Declaration declaration, IReadOnlyList<Argument> arguments, SourceAttributes read, AttributeSet target)
    {
        var texts = arguments.Where(argument => argument.Text is not null).Select(argument => argument.Text!).ToList();
        var step = arguments.Select(argument => argument.Number).FirstOrDefault(number => number is not null);
        switch (declaration.Name)
        {
            case "Requirement" when texts.Count == 2:
                read.Requirements.Add(new RequirementRecord(texts[0], texts[1], declaration.Path, declaration.Class));
                break;
            case "Feature" when texts.Count == 1:
                target.Feature = texts[0];
                break;
            case "Flow" when texts.Count == 1 && step is not null:
                target.Flows.Add(new FlowEntry(texts[0], step.Value));
                break;
        }
    }

    private static bool IsExpected(string attribute, Argument argument)
    {
        var isNamedStep = argument.Name == StepName;
        var isPositionalStep = argument.Name is null && argument.Position == StepPosition;
        var isStep = attribute == "Flow" && (isNamedStep || isPositionalStep);
        return isStep ? argument.Number is not null : argument.Text is not null;
    }

    private sealed record Declaration(string Source, string Path, Match Match, string Class)
    {
        public string Name => Match.Groups["attr"].Value;

        public AttributeProblem Problem(string argument) =>
            new(Path, Source[..Match.Index].Count(c => c == '\n') + 1, Name, argument);
    }

    [GeneratedRegex(Token)]
    private static partial Regex TokenPattern();
}
