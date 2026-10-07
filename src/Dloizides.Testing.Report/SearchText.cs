using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal static partial class SearchText
{
    private const int MessageLength = 300;

    [GeneratedRegex("[_ ]+|(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex WordBreak();

    public static string Build(TestResult test)
    {
        var message = test.Message.Length > MessageLength ? test.Message[..MessageLength] : test.Message;
        var methodWords = WordBreak().Replace(test.Method, " ");
        var parts = new[]
        {
            test.Name, methodWords, test.Scenario, test.Expected, test.Description,
            test.Feature, StatusText.Label(test.Status), message,
        };
        return string.Join(" ", parts.Where(p => p.Length > 0)).ToLowerInvariant();
    }
}
