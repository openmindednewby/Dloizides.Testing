using System.Globalization;
using System.Xml.Linq;

namespace Dloizides.Testing.Report;

internal static class TrxParser
{
    private static readonly string[] KnownFrameworks = ["nunit", "mstest", "xunit"];

    public static TrxFile Parse(string xml, TrxContext context, IReadOnlyDictionary<string, string> descriptions)
    {
        var root = XDocument.Parse(xml).Root ?? throw new InvalidDataException("The trx file has no root element.");
        var definitions = new Dictionary<string, XElement>(StringComparer.Ordinal);
        foreach (var unit in Children(root, "TestDefinitions").SelectMany(d => Children(d, "UnitTest")))
        {
            var method = Children(unit, "TestMethod").FirstOrDefault();
            if (method is not null)
                definitions[Attr(unit, "id")] = method;
        }

        var tests = Children(root, "Results")
            .SelectMany(r => Children(r, "UnitTestResult"))
            .Select(result => ToResult(result, definitions, context, descriptions))
            .ToList();
        var times = Children(root, "Times").FirstOrDefault();
        return new TrxFile(tests, Time(times, "start"), Time(times, "finish"));
    }

    public static string FeatureOf(string className, string project, string setName) => FeatureArea.Of(className, project, setName);

    private static TestResult ToResult(
        XElement result,
        Dictionary<string, XElement> definitions,
        TrxContext context,
        IReadOnlyDictionary<string, string> descriptions)
    {
        var testName = Attr(result, "testName");
        var paren = testName.IndexOf('(', StringComparison.Ordinal);
        var bare = paren >= 0 ? testName[..paren] : testName;
        definitions.TryGetValue(Attr(result, "testId"), out var definition);
        var className = definition is null ? BeforeLast(bare, '.') : Attr(definition, "className");
        var parts = MethodNameSplitter.Split(definition is null ? AfterLast(bare, '.') : Attr(definition, "name"));
        var shortClass = AfterLast(className, '.');
        var error = Children(result, "Output").SelectMany(o => Children(o, "ErrorInfo")).FirstOrDefault();
        return new TestResult
        {
            Name = testName,
            Framework = FrameworkOf(definition is null ? string.Empty : Attr(definition, "adapterTypeName")),
            Project = context.Project,
            Feature = FeatureOf(className, context.Project, context.SetName),
            Class = shortClass,
            Method = parts.Method,
            Description = descriptions.GetValueOrDefault(MethodDescriptionReader.Key(AfterLast(shortClass, '+'), parts.Method), string.Empty),
            Scenario = parts.Scenario,
            Expected = parts.Expected,
            Args = paren >= 0 ? testName[(paren + 1)..].TrimEnd(')') : string.Empty,
            Status = StatusText.FromOutcome(Attr(result, "outcome"), context.ExpectRed),
            Seconds = TimeSpan.TryParse(Attr(result, "duration"), CultureInfo.InvariantCulture, out var took) ? took.TotalSeconds : 0,
            Message = Text(error, "Message").Trim(),
            Stack = Text(error, "StackTrace").TrimEnd(),
        };
    }

    private static string FrameworkOf(string adapterTypeName) =>
        KnownFrameworks.FirstOrDefault(name => adapterTypeName.Contains(name, StringComparison.OrdinalIgnoreCase)) ?? ResultsJson.DefaultFramework;

    private static IEnumerable<XElement> Children(XElement parent, string localName) =>
        parent.Elements().Where(e => e.Name.LocalName == localName);

    private static string Attr(XElement element, string name) => element.Attribute(name)?.Value ?? string.Empty;

    private static string Text(XElement? parent, string localName) =>
        parent is null ? string.Empty : Children(parent, localName).FirstOrDefault()?.Value ?? string.Empty;

    private static DateTimeOffset? Time(XElement? times, string name) =>
        times is not null && DateTimeOffset.TryParse(Attr(times, name), CultureInfo.InvariantCulture, DateTimeStyles.None, out var value)
            ? value
            : null;

    private static string AfterLast(string text, char separator) => text[(text.LastIndexOf(separator) + 1)..];

    private static string BeforeLast(string text, char separator)
    {
        var index = text.LastIndexOf(separator);
        return index >= 0 ? text[..index] : text;
    }
}
