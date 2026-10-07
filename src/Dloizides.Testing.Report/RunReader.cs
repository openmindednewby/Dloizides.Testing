using System.Globalization;
using System.Text.Json;

namespace Dloizides.Testing.Report;

internal sealed record TrxLocation(string Base, string Path, string Link);

internal static class RunDates
{
    private static readonly string[] Formats = ["yyyyMMdd-HHmmss", "yyyy-MM-ddTHH-mm-ss"];

    public static DateTime? Parse(string folderName) =>
        DateTime.TryParseExact(folderName, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
}

internal sealed class RunReader(ReportOptions options, IReadOnlyDictionary<string, string> descriptions)
{
    private const int UnorderedSet = int.MaxValue;

    public TestRun Read(string folder)
    {
        var sets = new Dictionary<string, TestSet>(StringComparer.Ordinal);
        DateTimeOffset? start = null;
        DateTimeOffset? finish = null;
        foreach (var file in FindTrxFiles(folder))
        {
            var dash = file.Base.IndexOf('-', StringComparison.Ordinal);
            if (dash < 1)
                continue;
            var set = GetSet(sets, file.Base[..dash]);
            var project = file.Base[(dash + 1)..];
            var data = TrxParser.Parse(File.ReadAllText(file.Path), new TrxContext(set.Name, project, set.ExpectRed), descriptions);
            set.Tests.AddRange(data.Tests);
            var log = File.Exists(Path.Combine(folder, file.Base + ".log")) ? file.Base + ".log" : string.Empty;
            set.Files.Add(new SetFile(project, file.Link, log));
            start = data.Start is not null && (start is null || data.Start < start) ? data.Start : start;
            finish = data.Finish is not null && (finish is null || data.Finish > finish) ? data.Finish : finish;
        }

        var setFilter = ApplySummary(folder, sets);
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));
        var seconds = start is not null && finish is not null ? (finish.Value - start.Value).TotalSeconds : 0;
        return new TestRun(name, RunDates.Parse(name), Order(sets.Values), seconds, setFilter);
    }

    private static IEnumerable<TrxLocation> FindTrxFiles(string folder)
    {
        foreach (var trx in Directory.GetFiles(folder, "*.trx").Order(StringComparer.OrdinalIgnoreCase))
            yield return new TrxLocation(Path.GetFileNameWithoutExtension(trx), trx, Path.GetFileName(trx));
        foreach (var dir in Directory.GetDirectories(folder).Order(StringComparer.OrdinalIgnoreCase))
        {
            var inner = Directory.GetFiles(dir, "*.trx").Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            if (inner is null)
                continue;
            var dirName = Path.GetFileName(dir);
            yield return new TrxLocation(dirName, inner, $"{dirName}/{Path.GetFileName(inner)}");
        }
    }

    private TestSet GetSet(Dictionary<string, TestSet> sets, string name)
    {
        if (!sets.TryGetValue(name, out var set))
        {
            set = new TestSet(name, options.ExpectedRedSets.Contains(name));
            sets[name] = set;
        }

        return set;
    }

    private string ApplySummary(string folder, Dictionary<string, TestSet> sets)
    {
        var path = Path.Combine(folder, "summary.json");
        if (!File.Exists(path))
            return string.Empty;
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        if (root.TryGetProperty("Rows", out var rows) && rows.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in rows.EnumerateArray())
                MarkMissing(row, sets);
        }

        return root.TryGetProperty("Set", out var setFilter) && setFilter.ValueKind == JsonValueKind.String
            ? setFilter.GetString() ?? string.Empty
            : string.Empty;
    }

    private void MarkMissing(JsonElement row, Dictionary<string, TestSet> sets)
    {
        if (!row.TryGetProperty("Base", out var rowBase) || rowBase.ValueKind != JsonValueKind.String)
            return;
        var baseName = rowBase.GetString() ?? string.Empty;
        var dash = baseName.IndexOf('-', StringComparison.Ordinal);
        if (dash < 1)
            return;
        var set = GetSet(sets, baseName[..dash]);
        var project = baseName[(dash + 1)..];
        if (!set.Files.Any(f => f.Project == project))
            set.Missing.Add(project);
    }

    private List<TestSet> Order(IEnumerable<TestSet> sets) =>
        sets.OrderBy(s => OrderOf(s.Name)).ThenBy(s => s.Name, StringComparer.Create(CultureInfo.InvariantCulture, true)).ToList();

    private int OrderOf(string setName)
    {
        var index = options.SetOrder.ToList().IndexOf(setName);
        return index < 0 ? UnorderedSet : index;
    }
}
