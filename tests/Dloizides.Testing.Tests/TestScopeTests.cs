using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Dloizides.Testing.Tests;

[MethodUnderTest("Record", "Attributes each call to the test that caused it, also when the system under test or an async helper makes the call.")]
[RecordCalls]
public class TestScopeTests
{
    private const string Henex = "henex";

    [Fact]
    public async Task Record_WhenSutCallsOutThroughHttpClientFactory_AttributesCallToTest()
    {
        var expected = $"{typeof(TestScopeTests).FullName}.{nameof(Record_WhenSutCallsOutThroughHttpClientFactory_AttributesCallToTest)}";
        using var folder = new TempFolder();
        var log = new CallLog(folder.Path);
        await using var sut = await StartSut(log);
        using var client = sut.GetTestClient();

        using var response = await client.GetAsync(new Uri("/forward", UriKind.Relative));

        Assert.Equal([expected], TempFolder.RecordedTests(log));
    }

    [Fact]
    public async Task Record_WhenHelperCallsAfterAwait_AttributesCallToTest()
    {
        var expected = $"{typeof(TestScopeTests).FullName}.{nameof(Record_WhenHelperCallsAfterAwait_AttributesCallToTest)}";
        using var folder = new TempFolder();
        var log = new CallLog(folder.Path);

        await CallAfterYield(log);

        Assert.Equal([expected], TempFolder.RecordedTests(log));
    }

    private static async Task CallAfterYield(CallLog log)
    {
        await Task.Yield();
        using var henex = new HttpClient(new RecordingHandler("Trading", "HENEX", new FakeHenex(), log)) { BaseAddress = new Uri("http://henex.test") };
        using var response = await henex.GetAsync(new Uri("/results", UriKind.Relative));
    }

    private static async Task<WebApplication> StartSut(CallLog log)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer(options => options.PreserveExecutionContext = true);
        builder.Services.AddHttpClient(Henex, client => client.BaseAddress = new Uri("http://henex.test"))
            .ConfigurePrimaryHttpMessageHandler(() => new FakeHenex())
            .AddHttpMessageHandler(() => new RecordingHandler("Trading", "HENEX", log: log));
        var app = builder.Build();
        app.MapGet("/forward", async (IHttpClientFactory factory) =>
        {
            using var reply = await factory.CreateClient(Henex).GetAsync(new Uri("/results", UriKind.Relative));
            return (int)reply.StatusCode;
        });
        await app.StartAsync();
        return app;
    }
}
