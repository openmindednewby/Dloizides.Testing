using Shouldly;

namespace Dloizides.Testing.Security.Tests;

public sealed class SecurityProbeMustCatchTests : IClassFixture<LeakyHostFailures>
{
    private readonly LeakyHostFailures _leaky;

    public SecurityProbeMustCatchTests(LeakyHostFailures leaky)
    {
        _leaky = leaky;
    }

    [Fact]
    public void FindFailuresAsync_WithoutJwtValidation_ReportsEveryBadToken()
    {
        var failures = _leaky.Of(SecurityProbeKind.JwtValidation);

        failures.ShouldBe([
            "GET /items with a forged signature => 200",
            "GET /items with an expired token => 200",
            "GET /items with a wrong audience => 200",
            "GET /items with alg none => 200",
        ]);
    }

    [Fact]
    public void FindFailuresAsync_WithoutFallbackPolicy_ReportsAnonymousAccess()
    {
        var failures = _leaky.Of(SecurityProbeKind.FallbackPolicy);

        failures.ShouldBe(["GET /items without a token => 200"]);
    }

    [Fact]
    public void FindFailuresAsync_WithoutTenantFilter_ReportsEveryCrossTenantRequest()
    {
        var failures = _leaky.Of(SecurityProbeKind.CrossTenant);

        failures.ShouldBe([
            "GET /tenants/other/items/1 with a valid token => 200",
            "PATCH /tenants/other/items/1 with a valid token => 204",
            "DELETE /tenants/other/items/1 with a valid token => 200",
        ]);
    }

    [Fact]
    public void FindFailuresAsync_WithoutTenantFilter_ExpectsForbiddenOrNotFound()
    {
        var expected = _leaky.ExpectedOf(SecurityProbeKind.CrossTenant);

        expected.ShouldBe(["403 or 404", "403 or 404", "403 or 404"]);
    }

    [Fact]
    public void FindFailuresAsync_WithDeveloperExceptionPage_ReportsLeakedDetails()
    {
        var expected = _leaky.ExpectedOf(SecurityProbeKind.ErrorDetails);

        expected.ShouldBe(["application/problem+json", "no exception message", "no stack trace"]);
    }

    [Fact]
    public void FindFailuresAsync_WithoutSecurityHeaders_ReportsEveryMissingHeader()
    {
        var expected = _leaky.ExpectedOf(SecurityProbeKind.SecurityHeaders);

        expected.ShouldBe([
            "X-Content-Type-Options: nosniff",
            "Content-Security-Policy with frame-ancestors 'none'",
            "Referrer-Policy",
            "Permissions-Policy",
            "Strict-Transport-Security",
        ]);
    }

    [Fact]
    public void FindFailuresAsync_WithoutBodyLimit_ReportsOversizedBodyAccepted()
    {
        var failures = _leaky.Of(SecurityProbeKind.BodyLimit);

        failures.ShouldBe(["POST /items with 4097 bytes => 200"]);
    }

    [Fact]
    public void FindFailuresAsync_WithoutRateLimit_ReportsNoThrottledRequest()
    {
        var failures = _leaky.Of(SecurityProbeKind.RateLimit);

        failures.ShouldBe(["150 x GET /items without a token => none"]);
    }

    [Fact]
    public void FindFailuresAsync_WithAnyOriginCors_ReportsAllowOriginHeader()
    {
        var failures = _leaky.Of(SecurityProbeKind.Cors);

        failures.ShouldBe([
            "GET /items from https://unknown-origin.invalid => Access-Control-Allow-Origin: *",
            "OPTIONS /items from https://unknown-origin.invalid => Access-Control-Allow-Origin: *",
        ]);
    }

    [Fact]
    public void FindFailuresAsync_WithPublicMetrics_ReportsMetricsReachable()
    {
        var failures = _leaky.Of(SecurityProbeKind.ManagementPort);

        failures.ShouldBe(["GET /metrics => 200"]);
    }
}
