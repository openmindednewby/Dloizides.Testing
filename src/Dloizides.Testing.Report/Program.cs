namespace Dloizides.Testing.Report;

internal static class Program
{
    private const int InvalidResults = 1;
    private const int UsageError = 2;

    internal static int Run(IReadOnlyList<string> args, TextWriter output, TextWriter error)
    {
        var parsed = OptionsParser.Parse(args);
        if (parsed.IsHelp)
        {
            output.WriteLine(OptionsParser.Usage);
            return 0;
        }

        if (parsed.Options is null)
        {
            error.WriteLine(parsed.Error);
            error.WriteLine(OptionsParser.Usage);
            return UsageError;
        }

        if (!Directory.Exists(parsed.Options.RunFolder))
        {
            error.WriteLine($"Run folder not found: {parsed.Options.RunFolder}");
            return UsageError;
        }

        if (parsed.Options.ResultsFile is { } results && !File.Exists(results))
        {
            error.WriteLine($"Results file not found: {results}");
            return UsageError;
        }

        try
        {
            output.WriteLine($"Report: {ReportGenerator.Generate(parsed.Options)}");
            return 0;
        }
        catch (ResultsJsonException exception)
        {
            error.WriteLine(exception.Message);
            return InvalidResults;
        }
    }

    private static int Main(string[] args) => Run(args, Console.Out, Console.Error);
}
