namespace Dloizides.Testing.Report;

internal static class Program
{
    private const int UsageError = 2;

    private static int Main(string[] args)
    {
        var parsed = OptionsParser.Parse(args);
        if (parsed.IsHelp)
        {
            Console.WriteLine(OptionsParser.Usage);
            return 0;
        }

        if (parsed.Options is null)
        {
            Console.Error.WriteLine(parsed.Error);
            Console.Error.WriteLine(OptionsParser.Usage);
            return UsageError;
        }

        if (!Directory.Exists(parsed.Options.RunFolder))
        {
            Console.Error.WriteLine($"Run folder not found: {parsed.Options.RunFolder}");
            return UsageError;
        }

        Console.WriteLine($"Report: {ReportGenerator.Generate(parsed.Options)}");
        return 0;
    }
}
