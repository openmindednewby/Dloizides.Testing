namespace Dloizides.Testing.Security;

/// <summary>What a service hands the probe: a client on its public HTTPS address, paths, a token factory.</summary>
public sealed class SecurityProbeTarget
{
    /// <summary>The default body limit, 1 MiB.</summary>
    public const long DefaultMaxRequestBodyBytes = 1_048_576;

    /// <summary>The default number of requests in the burst.</summary>
    public const int DefaultBurstRequests = 150;

    /// <summary>The default public scrape path.</summary>
    public const string DefaultMetricsPath = "/metrics";

    /// <summary>The default origin no service allows.</summary>
    public const string DefaultUnknownOrigin = "https://unknown-origin.invalid";

    /// <summary>A client on the service's public HTTPS address.</summary>
    public required HttpClient Client { get; init; }

    /// <summary>The audience a valid token carries.</summary>
    public required string Audience { get; init; }

    /// <summary>Creates tokens the service accepts.</summary>
    public required ProbeTokenFactory CreateToken { get; init; }

    /// <summary>A GET endpoint not marked AllowAnonymous.</summary>
    public required string ProtectedPath { get; init; }

    /// <summary>A GET endpoint that throws with <see cref="SecurityProbe.ForcedExceptionMessage"/>.</summary>
    public required string ThrowingPath { get; init; }

    /// <summary>A POST endpoint that reads its body.</summary>
    public required string BodyPath { get; init; }

    /// <summary>The service's request body limit; the probe sends one byte more.</summary>
    public long MaxRequestBodyBytes { get; init; } = DefaultMaxRequestBodyBytes;

    /// <summary>The number of anonymous requests sent to the protected path, expecting a 429.</summary>
    public int BurstRequests { get; init; } = DefaultBurstRequests;

    /// <summary>The scrape path that must answer 404 on the public address.</summary>
    public string MetricsPath { get; init; } = DefaultMetricsPath;

    /// <summary>An origin the service does not allow.</summary>
    public string UnknownOrigin { get; init; } = DefaultUnknownOrigin;

    /// <summary>Requests to another tenant's records, sent with a valid token; each must answer 403 or 404.</summary>
    public IReadOnlyList<CrossTenantRequest> CrossTenantRequests { get; init; } = [];
}
