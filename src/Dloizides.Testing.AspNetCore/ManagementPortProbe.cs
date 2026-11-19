using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace Dloizides.Testing.AspNetCore;

/// <summary>
/// Probes a test host for MGMT-PORT-1: every management path answers 404 on the public port and 200 on the management port.
/// </summary>
public static class ManagementPortProbe
{
    /// <summary>The public port, 8080.</summary>
    public const int DefaultPublicPort = 8080;

    /// <summary>The management port, 8081.</summary>
    public const int DefaultManagementPort = 8081;

    /// <summary>The scrape path and the three health probe paths.</summary>
    public static IReadOnlyList<string> DefaultPaths { get; } = ["/metrics", "/health/live", "/health/start", "/health/ready"];

    /// <summary>Returns every probe whose status differs from 404 on the public port or 200 on the management port.</summary>
    public static async Task<IReadOnlyList<ManagementPortProbeFailure>> FindFailuresAsync(
        TestServer server,
        IReadOnlyList<string>? paths = null,
        int publicPort = DefaultPublicPort,
        int managementPort = DefaultManagementPort)
    {
        var expectations = (paths ?? DefaultPaths).SelectMany(path => new[]
        {
            new Expectation(path, publicPort, HttpStatusCode.NotFound),
            new Expectation(path, managementPort, HttpStatusCode.OK),
        });
        var failures = new List<ManagementPortProbeFailure>();
        foreach (var expectation in expectations)
        {
            var actual = await GetStatusAsync(server, expectation.Port, expectation.Path);
            if (actual != expectation.Status)
                failures.Add(new ManagementPortProbeFailure(expectation.Path, expectation.Port, expectation.Status, actual));
        }
        return failures;
    }

    /// <summary>Throws <see cref="ManagementPortProbeException"/> listing every failed probe.</summary>
    public static async Task EnsureManagementPortOnlyAsync(
        TestServer server,
        IReadOnlyList<string>? paths = null,
        int publicPort = DefaultPublicPort,
        int managementPort = DefaultManagementPort)
    {
        var failures = await FindFailuresAsync(server, paths, publicPort, managementPort);
        if (failures.Count > 0)
            throw new ManagementPortProbeException(failures);
    }

    private static async Task<HttpStatusCode> GetStatusAsync(TestServer server, int localPort, string path)
    {
        var context = await server.SendAsync(request =>
        {
            request.Connection.LocalPort = localPort;
            request.Request.Method = HttpMethods.Get;
            request.Request.Path = path;
            request.Request.Host = new HostString("localhost");
        });
        return (HttpStatusCode)context.Response.StatusCode;
    }

    private sealed record Expectation(string Path, int Port, HttpStatusCode Status);
}
