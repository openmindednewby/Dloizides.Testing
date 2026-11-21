namespace Dloizides.Testing.Security;

/// <summary>The control a probe checks.</summary>
public enum SecurityProbeKind
{
    /// <summary>Forged, expired, wrong-audience and alg none tokens answer 401.</summary>
    JwtValidation,

    /// <summary>An endpoint not marked AllowAnonymous answers 401 without a token.</summary>
    FallbackPolicy,

    /// <summary>Another tenant's record answers 404.</summary>
    CrossTenant,

    /// <summary>A forced exception answers ProblemDetails with no stack trace and no exception message.</summary>
    ErrorDetails,

    /// <summary>nosniff, CSP with frame-ancestors 'none', Referrer-Policy, Permissions-Policy, HSTS.</summary>
    SecurityHeaders,

    /// <summary>A body over the limit answers 413.</summary>
    BodyLimit,

    /// <summary>A burst of requests gets at least one 429.</summary>
    RateLimit,

    /// <summary>An unknown origin gets no CORS header.</summary>
    Cors,

    /// <summary>The scrape path answers 404 on the public address.</summary>
    ManagementPort,
}
