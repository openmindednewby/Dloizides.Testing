using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal static partial class AttributeReader
{
    private const string Arguments = """
        (?<args>(?:"(?:[^"\\]|\\.)*"|[^()"]|(?<open>\()|(?<-open>\)))*(?(open)(?!)))
        """;

    private const string Generic = @"(?:<[\w\s,.?<>\[\]]*>)?";

    private const string TypeStart =
        @"(?<=(?:^|[;{}\]]|\b(?:public|internal|private|protected|sealed|abstract|static|partial|file|readonly|ref|unsafe|new))\s*)";

    private const string Token = @"(?<=[\[,]\s*)(?:global::)?(?:\w+\.)*(?<attr>Requirement|Covers|Feature|Flow)(?:Attribute)?\s*\(" + Arguments +
        @"\)|" + TypeStart + @"(?:record\s+(?:(?:class|struct)\s+)?|class\s+|struct\s+|interface\s+)(?<class>\w+)|\b(?:void|Task|ValueTask)" +
        Generic + @"\s+(?<method>\w+)\s*" + Generic + @"\s*\(|[{};]";

    private const string ExtraBrace = "extra }";
    private const string MissingBrace = "missing }";
    private const char NewLine = (char)0x0A;
    private const string StepName = "step";
    private const string IdName = "id";
    private const string TextName = "text";
    private const string NameName = "name";
    private const int FirstPosition = 0;
    private const int StepPosition = 1;
    private const int TextPosition = 1;
    private const int OneArgument = 1;
    private const int TwoArguments = 2;

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
        var scope = new ClassScope();
        var braceReported = false;
        foreach (Match match in TokenPattern().Matches(SourceMask.Apply(source)))
        {
            if (match.Groups["attr"].Success)
            {
                pending.Add(match);
                continue;
            }

            if (match.Groups["class"].Success)
            {
                var className = match.Groups["class"].Value;
                scope.Declare(className);
                Flush(pending, new Declared(source, path, className), read, Target(read.Classes, className));
                continue;
            }

            if (match.Groups["method"].Success)
            {
                var key = MethodDescriptionReader.Key(scope.Current, match.Groups["method"].Value);
                Flush(pending, new Declared(source, path, scope.Current), read, Target(read.Methods, key));
                continue;
            }

            scope.Step(match.Value);
            if (match.Value is "{" or ";")
                pending.Clear();
            if (scope.Depth < 0 && !braceReported)
                braceReported = AddBraceProblem(read, new Declared(source, path, scope.Current), match.Index, ExtraBrace);
        }

        if (scope.Depth > 0 && !braceReported)
            AddBraceProblem(read, new Declared(source, path, scope.Current), source.Length, MissingBrace);
    }

    private static bool AddBraceProblem(SourceAttributes read, Declared owner, int index, string problem)
    {
        read.Problems.Add(new AttributeProblem(owner.Path, LineAt(owner.Source, index), AttributeProblem.Braces, problem));
        return true;
    }

    private static int LineAt(string source, int index) => source[..index].Count(c => c == NewLine) + 1;

    private static void Flush(List<Match> pending, Declared owner, SourceAttributes read, AttributeSet target)
    {
        foreach (var attribute in pending)
            Apply(new Declaration(owner.Source, owner.Path, attribute, owner.Class), read, target);
        pending.Clear();
    }

    private static AttributeSet Target(Dictionary<string, AttributeSet> sets, string key)
    {
        if (!sets.TryGetValue(key, out var set))
            sets[key] = set = new AttributeSet();
        return set;
    }

    private static void Apply(Declaration declaration, SourceAttributes read, AttributeSet target)
    {
        var arguments = ArgumentSplitter.Split(declaration.Arguments);
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
        var first = Bound(arguments, declaration.Name == "Requirement" ? IdName : NameName, FirstPosition);
        switch (declaration.Name)
        {
            case "Requirement" when arguments.Count == TwoArguments && first?.Text is { } id && Bound(arguments, TextName, TextPosition)?.Text is { } text:
                read.Requirements.Add(new RequirementRecord(id, text, declaration.Path, declaration.Class));
                break;
            case "Feature" when arguments.Count == OneArgument && first?.Text is { } feature:
                target.Feature = feature;
                break;
            case "Flow" when arguments.Count == TwoArguments && first?.Text is { } flow && Bound(arguments, StepName, StepPosition)?.Number is { } step:
                target.Flows.Add(new FlowEntry(flow, step));
                break;
        }
    }

    private static Argument? Bound(IReadOnlyList<Argument> arguments, string name, int position) =>
        arguments.FirstOrDefault(argument => argument.Name == name) ??
        arguments.FirstOrDefault(argument => argument.Name is null && argument.Position == position);

    private static bool IsExpected(string attribute, Argument argument)
    {
        var isNamedStep = argument.Name == StepName;
        var isPositionalStep = argument.Name is null && argument.Position == StepPosition;
        var isStep = attribute == "Flow" && (isNamedStep || isPositionalStep);
        return isStep ? argument.Number is not null : argument.Text is not null;
    }

    private sealed record Declared(string Source, string Path, string Class);

    private sealed record Declaration(string Source, string Path, Match Match, string Class)
    {
        public string Name => Match.Groups["attr"].Value;

        public string Arguments => Source.Substring(Match.Groups["args"].Index, Match.Groups["args"].Length);

        public AttributeProblem Problem(string argument) =>
            new(Path, LineAt(Source, Match.Index), Name, argument);
    }

    [GeneratedRegex(Token)]
    private static partial Regex TokenPattern();
}
