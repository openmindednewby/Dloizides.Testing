using System.Net;
using System.Reflection;
using System.Text.Json;
using Xunit.Sdk;

namespace Dloizides.Testing.Tests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RecordCallsAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest) => TestScope.Enter(methodUnderTest);

    public override void After(MethodInfo methodUnderTest) => TestScope.Exit();
}

internal sealed class FakeHenex : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
}

internal sealed class TempFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "testdoc-calls-" + Guid.NewGuid().ToString("N"));

    public static IReadOnlyList<string?> RecordedTests(CallLog log) =>
        File.ReadLines(log.FilePath).Select(line => JsonDocument.Parse(line).RootElement.GetProperty("test").GetString()).ToList();

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}
