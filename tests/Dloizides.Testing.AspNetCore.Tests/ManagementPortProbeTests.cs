using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;

namespace Dloizides.Testing.AspNetCore.Tests;

public sealed class ManagementPortProbeTests
{
    private const int PublicPort = 8080;
    private const int ManagementPort = 8081;

    [Fact]
    public async Task FindFailuresAsync_WithManagementOnlyHost_ReturnsNoFailures()
    {
        await using var app = await StartAsync(context => context.Connection.LocalPort == ManagementPort ? StatusCodes.Status200OK : StatusCodes.Status404NotFound);

        var failures = await ManagementPortProbe.FindFailuresAsync(app.GetTestServer());

        failures.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindFailuresAsync_WithEndpointsOnPublicPort_ReturnsEveryPathOnPublicPort()
    {
        await using var app = await StartAsync(_ => StatusCodes.Status200OK);

        var failures = await ManagementPortProbe.FindFailuresAsync(app.GetTestServer());

        failures.Select(f => $"{f.Path}:{f.Port}:{(int)f.Actual}").ShouldBe(
            ["/metrics:8080:200", "/health/live:8080:200", "/health/start:8080:200", "/health/ready:8080:200"]);
    }

    [Fact]
    public async Task FindFailuresAsync_WithoutManagementEndpoints_ReturnsManagementPortFailure()
    {
        await using var app = await StartAsync(_ => StatusCodes.Status404NotFound);

        var failures = await ManagementPortProbe.FindFailuresAsync(app.GetTestServer(), ["/metrics"]);

        failures.ShouldHaveSingleItem().ShouldBe(new ManagementPortProbeFailure("/metrics", ManagementPort, HttpStatusCode.OK, HttpStatusCode.NotFound));
    }

    [Fact]
    public async Task EnsureManagementPortOnlyAsync_WithEndpointsOnPublicPort_ThrowsNamingFailedProbe()
    {
        await using var app = await StartAsync(_ => StatusCodes.Status200OK);

        var exception = await Should.ThrowAsync<ManagementPortProbeException>(() => ManagementPortProbe.EnsureManagementPortOnlyAsync(app.GetTestServer(), ["/metrics"]));

        exception.Message.ShouldContain($"GET /metrics on port {PublicPort}: expected 404, got 200");
    }

    private static async Task<WebApplication> StartAsync(Func<HttpContext, int> status)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.Run(context =>
        {
            context.Response.StatusCode = status(context);
            return Task.CompletedTask;
        });
        await app.StartAsync();
        return app;
    }
}
