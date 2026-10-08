using System.Text.RegularExpressions;

namespace Dloizides.Testing.Report;

internal static partial class IdGrammar
{
    public const string Pattern = "^[A-Za-z][A-Za-z0-9_.-]*$";

    public static bool IsValid(string id) => IdPattern().IsMatch(id);

    [GeneratedRegex(Pattern)]
    private static partial Regex IdPattern();
}
