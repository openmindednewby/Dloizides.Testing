using Shouldly;

namespace Dloizides.Testing.Security.Tests;

public sealed class SecurityProbeMustAllowTests
{
    [Fact]
    public async Task FindFailuresAsync_WithServiceDefaultsHost_ReturnsNoFailures()
    {
        await using var host = await ProbeHost.StartServiceDefaultsAsync();

        var failures = await SecurityProbe.FindFailuresAsync(host.Target);

        failures.Select(failure => failure.ToString()).ShouldBeEmpty();
    }

    [Fact]
    public async Task FindFailuresAsync_WithLargeBodyLimit_ReadsThe413()
    {
        const long largeLimit = 26L * 1024 * 1024;
        await using var host = await ProbeHost.StartServiceDefaultsAsync(largeLimit);

        var failures = await SecurityProbe.FindFailuresAsync(host.Target);

        failures.Select(failure => failure.ToString()).ShouldBeEmpty();
    }

    [Fact]
    public async Task EnsureSecureAsync_WithLeakyHost_ThrowsNamingFailedProbe()
    {
        await using var host = await ProbeHost.StartLeakyAsync();

        var exception = await Should.ThrowAsync<SecurityProbeException>(() => SecurityProbe.EnsureSecureAsync(host.Target));

        exception.Message.ShouldContain("FallbackPolicy: GET /items without a token: expected 401, got 200");
    }
}
