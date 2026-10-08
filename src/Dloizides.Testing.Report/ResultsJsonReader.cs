using System.Globalization;
using System.Text.Json;

namespace Dloizides.Testing.Report;

internal static class ResultsJsonReader
{
    private const string SchemaField = "schema";
    private const string Root = "(root)";

    public static TestRun Read(string json)
    {
        using var parsed = Parse(json);
        var root = parsed.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new ResultsJsonException(Root, "must be a JSON object");
        CheckSchema(root);
        var run = Required(root, "run", JsonValueKind.Object, "run");
        Required(run, "name", JsonValueKind.String, "run.name");
        Required(root, "requirements", JsonValueKind.Array, "requirements");
        var tests = Required(root, "tests", JsonValueKind.Array, "tests");
        var index = 0;
        foreach (var test in tests.EnumerateArray())
            CheckTest(test, $"tests[{index++}]");

        var document = root.Deserialize<ResultsDocument>(ResultsJson.Options)
            ?? throw new ResultsJsonException(Root, "could not be read");
        return ToRun(document);
    }

    private static JsonDocument Parse(string json)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new ResultsJsonException("(document)", $"is not valid JSON: {exception.Message}");
        }
    }

    private static void CheckSchema(JsonElement root)
    {
        if (!root.TryGetProperty(SchemaField, out var schema) || schema.ValueKind != JsonValueKind.String)
            throw new ResultsJsonException(SchemaField, "is missing");
        var value = schema.GetString() ?? string.Empty;
        var rest = value.StartsWith(ResultsJson.SchemaPrefix, StringComparison.Ordinal) ? value[ResultsJson.SchemaPrefix.Length..] : string.Empty;
        var dot = rest.IndexOf('.', StringComparison.Ordinal);
        var major = dot >= 0 ? rest[..dot] : rest;
        if (major != ResultsJson.SupportedMajor)
            throw new ResultsJsonException(SchemaField, $"has unknown major version \"{value}\"; this reader understands {ResultsJson.SchemaName}");
    }

    private static void CheckTest(JsonElement test, string path)
    {
        if (test.ValueKind != JsonValueKind.Object)
            throw new ResultsJsonException(path, "must be an object");
        Required(test, "id", JsonValueKind.String, $"{path}.id");
        Required(test, "set", JsonValueKind.String, $"{path}.set");
        var status = Required(test, "status", JsonValueKind.String, $"{path}.status").GetString() ?? string.Empty;
        if (!ResultsJson.TryStatus(status, out _))
            throw new ResultsJsonException($"{path}.status", $"has unknown value \"{status}\"; expected pass, fail, skip, xfail or xpass");
    }

    private static JsonElement Required(JsonElement parent, string name, JsonValueKind kind, string path)
    {
        if (!parent.TryGetProperty(name, out var value))
            throw new ResultsJsonException(path, "is missing");
        if (value.ValueKind != kind)
            throw new ResultsJsonException(path, $"must be {kind.ToString().ToLowerInvariant()}, got {value.ValueKind.ToString().ToLowerInvariant()}");
        return value;
    }

    private static TestRun ToRun(ResultsDocument document)
    {
        var sets = new List<TestSet>();
        foreach (var declared in document.Run.Sets)
        {
            var set = new TestSet(declared.Name, declared.ExpectRed);
            set.Files.AddRange(declared.Files.Select(f => new SetFile(f.Project, f.Trx, f.Log)));
            set.Missing.AddRange(declared.Missing);
            sets.Add(set);
        }

        foreach (var test in document.Tests)
        {
            var set = sets.Find(s => s.Name == test.Set);
            if (set is null)
            {
                set = new TestSet(test.Set, test.ExpectRed);
                sets.Add(set);
            }

            set.Tests.Add(ToResult(test));
        }

        var name = document.Run.Name;
        return new TestRun(name, RunDates.Parse(name), sets, Seconds(document.Run), document.Run.SetFilter);
    }

    private static double Seconds(ResultsRun run)
    {
        if (run.Seconds > 0)
            return run.Seconds;
        var hasStart = DateTimeOffset.TryParse(run.StartedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start);
        var hasFinish = DateTimeOffset.TryParse(run.FinishedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var finish);
        return hasStart && hasFinish ? (finish - start).TotalSeconds : 0;
    }

    private static TestResult ToResult(ResultsTest test)
    {
        ResultsJson.TryStatus(test.Status, out var status);
        return new TestResult
        {
            Name = test.Id,
            Project = test.Project,
            Feature = test.Feature,
            Class = test.Class,
            Method = test.Method,
            Description = test.Description,
            Scenario = test.Scenario,
            Expected = test.Expected,
            Args = test.Args,
            Status = status,
            Seconds = test.Seconds,
            Message = test.Message,
            Stack = test.Stack,
        };
    }
}
