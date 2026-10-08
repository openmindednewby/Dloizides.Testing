using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Dloizides.Testing.Report;

namespace Dloizides.Testing.Tests;

[MethodUnderTest("AC16", "Records every call through a wrapped fake server in order and draws one sequence arrow per call.")]
public partial class RecordingHandlerTests
{
    private const string TestClass = "Dloizides.Testing.Tests.RecordingHandlerTests";

    [Fact]
    public async Task AC16_WithTwoCallsThroughFakeServer_RecordsAndDrawsBothInOrder()
    {
        const string first = "/declarations";
        const string second = "/results";
        var runFolder = Path.Combine(Path.GetTempPath(), "testdoc-calls-" + Guid.NewGuid().ToString("N"), "20261009-100000");
        Directory.CreateDirectory(runFolder);
        File.WriteAllText(Path.Combine(runFolder, "Unit-Trading.Tests.trx"), Trx($"{TestClass}.{nameof(AC16_WithTwoCallsThroughFakeServer_RecordsAndDrawsBothInOrder)}"));
        var log = new CallLog(Path.Combine(runFolder, RecordedCalls.Folder));
        using var henex = new HttpClient(new RecordingHandler("Trading", "HENEX", new FakeHenex(), log)) { BaseAddress = new Uri("http://henex.test") };
        using var posted = await henex.PostAsync(first, content: null);
        using var fetched = await henex.GetAsync(second);
        var reader = new RunReader(new ReportOptions(runFolder, [], new HashSet<string>(), [], string.Empty), new Dictionary<string, string>());

        var json = ResultsJsonWriter.Write(RecordedCalls.Attach(reader.Read(runFolder), runFolder));

        var calls = JsonNode.Parse(json)!["tests"]![0]!["calls"]!.AsArray();
        var sequence = SequenceDiagram.Render(ResultsJsonReader.Read(json).Sets.SelectMany(set => set.Tests).ToList()).Single().Mermaid;
        var arrows = RequestArrow().Matches(sequence).Select(match => match.Groups["message"].Value).ToList();
        Assert.Equal([1, 2], calls.Select(call => call!["seq"]!.GetValue<int>()));
        Assert.Equal([first, second], calls.Select(call => call!["path"]!.GetValue<string>()));
        Assert.Equal(["HENEX", "HENEX"], calls.Select(call => call!["to"]!.GetValue<string>()));
        Assert.Equal([$"POST {first}", $"GET {second}"], arrows);
    }

    [GeneratedRegex(@"^\s*p\d+->>p\d+: (?<message>.+)$", RegexOptions.Multiline)]
    private static partial Regex RequestArrow();

    private static string Trx(string testName) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">"
        + "<Times start=\"2026-10-09T10:00:00.0000000+00:00\" finish=\"2026-10-09T10:00:01.0000000+00:00\" /><Results>"
        + $"<UnitTestResult testId=\"id-0\" testName=\"{testName}\" outcome=\"Passed\" duration=\"00:00:01.0000000\" /></Results><TestDefinitions>"
        + $"<UnitTest id=\"id-0\" name=\"{testName}\"><TestMethod className=\"{TestClass}\" name=\"{testName[(testName.LastIndexOf('.') + 1)..]}\" /></UnitTest>"
        + "</TestDefinitions></TestRun>";

    private sealed class FakeHenex : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
