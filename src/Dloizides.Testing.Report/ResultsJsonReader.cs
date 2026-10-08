using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace Dloizides.Testing.Report;

internal sealed record SchemaFailure(string Location, IReadOnlyDictionary<string, string>? Errors);

internal static class ResultsJsonReader
{
    private const string SchemaField = "schema";
    private const string Root = "(root)";
    private const string Document = "(document)";
    private const string SchemaResource = "testdoc-results.v1.schema.json";
    private const string JsonPathPrefix = "$.";

    private static readonly Lazy<JsonSchema> Contract = new(LoadContract);

    public static TestRun Read(string json)
    {
        var root = Parse(json);
        if (root is not JsonObject document)
            throw new ResultsJsonException(Root, "must be a JSON object");
        CheckSchema(document);
        CheckContract(document);
        return ToRun(Deserialize(document));
    }

    private static JsonNode? Parse(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new ResultsJsonException(Document, $"is not valid JSON: {exception.Message}");
        }
    }

    private static void CheckSchema(JsonObject root)
    {
        if (!root.TryGetPropertyValue(SchemaField, out var schema) || schema is not JsonValue value || !value.TryGetValue<string>(out var text))
            throw new ResultsJsonException(SchemaField, "is missing");
        var rest = text.StartsWith(ResultsJson.SchemaPrefix, StringComparison.Ordinal) ? text[ResultsJson.SchemaPrefix.Length..] : string.Empty;
        var dot = rest.IndexOf('.', StringComparison.Ordinal);
        var major = dot >= 0 ? rest[..dot] : rest;
        if (major != ResultsJson.SupportedMajor)
            throw new ResultsJsonException(SchemaField, $"has unknown major version \"{text}\"; this reader understands {ResultsJson.SchemaName}");
    }

    private static void CheckContract(JsonObject root)
    {
        var options = new EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = true };
        var result = Contract.Value.Evaluate(root, options);
        if (result.IsValid)
            return;
        throw Describe(new[] { result }.Concat(result.Details)
            .Select(detail => new SchemaFailure(detail.InstanceLocation.ToString(), detail.Errors)));
    }

    internal static ResultsJsonException Describe(IEnumerable<SchemaFailure> failures)
    {
        var failure = failures
            .Where(detail => detail.Errors is { Count: > 0 })
            .OrderByDescending(detail => Segments(detail.Location).Length)
            .FirstOrDefault();
        return failure is null
            ? new ResultsJsonException(Root, "breaks the schema")
            : new ResultsJsonException(FieldName(failure.Location), $"breaks the schema: {string.Join("; ", failure.Errors!.Values)}");
    }

    private static ResultsDocument Deserialize(JsonObject root)
    {
        try
        {
            return root.Deserialize<ResultsDocument>(ResultsJson.Options) ?? throw new ResultsJsonException(Root, "could not be read");
        }
        catch (JsonException exception)
        {
            var path = exception.Path is { Length: > 0 } jsonPath ? jsonPath.Replace(JsonPathPrefix, string.Empty, StringComparison.Ordinal) : Document;
            throw new ResultsJsonException(path, $"could not be read: {exception.Message}");
        }
    }

    private static string FieldName(string pointer)
    {
        var segments = Segments(pointer);
        if (segments.Length == 0)
            return Root;
        return string.Concat(segments.Select((segment, index) => IsIndex(segment) ? $"[{segment}]" : (index == 0 ? segment : "." + segment)));
    }

    private static string[] Segments(string pointer) =>
        pointer.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal))
            .ToArray();

    private static bool IsIndex(string segment) => int.TryParse(segment, NumberStyles.None, CultureInfo.InvariantCulture, out _);

    private static JsonSchema LoadContract()
    {
        using var stream = typeof(ResultsJsonReader).Assembly.GetManifestResourceStream(SchemaResource)
            ?? throw new InvalidOperationException($"Embedded schema {SchemaResource} is missing.");
        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
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

        var run = document.Run;
        return new TestRun(run.Name, RunDates.Parse(run.Name), sets, Seconds(run), run.SetFilter)
        {
            Requirements = document.Requirements,
            Features = document.Features ?? [],
            Repo = run.Repo,
            Sha = run.Sha,
            StartedAt = run.StartedAt,
            FinishedAt = run.FinishedAt,
        };
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
            Framework = test.Framework,
            Project = test.Project,
            Feature = test.Feature,
            Class = test.Class,
            Method = test.Method,
            Description = test.Description,
            Scenario = test.Scenario,
            Expected = test.Expected,
            Args = test.Args,
            Covers = test.Covers,
            Flows = test.Flows.Select(f => new FlowEntry(f.Name, f.Step)).ToList(),
            Calls = test.Calls,
            UseCases = test.UseCases ?? [],
            Status = status,
            Seconds = test.Seconds,
            Message = test.Message,
            Stack = test.Stack,
        };
    }
}
