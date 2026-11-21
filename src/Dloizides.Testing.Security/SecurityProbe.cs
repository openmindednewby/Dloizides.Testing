namespace Dloizides.Testing.Security;

/// <summary>
/// Probes a running service for the security floor: tokens, fallback policy, errors, headers, limits, CORS, /metrics.
/// </summary>
public static class SecurityProbe
{
    /// <summary>The message the service's throwing endpoint carries; it must never reach the response.</summary>
    public const string ForcedExceptionMessage = "security-probe-forced-exception";

    /// <summary>Runs every probe, the burst last, and returns each control the service is missing.</summary>
    public static async Task<IReadOnlyList<SecurityProbeFailure>> FindFailuresAsync(
        SecurityProbeTarget target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        return
        [
            .. await AuthenticationProbes.RunAsync(target, cancellationToken),
            .. await ResponseProbes.RunAsync(target, cancellationToken),
            .. await ResourceLimitProbes.RunAsync(target, cancellationToken),
        ];
    }

    /// <summary>Throws <see cref="SecurityProbeException"/> listing every control the service is missing.</summary>
    public static async Task EnsureSecureAsync(SecurityProbeTarget target, CancellationToken cancellationToken = default)
    {
        var failures = await FindFailuresAsync(target, cancellationToken);
        if (failures.Count > 0)
            throw new SecurityProbeException(failures);
    }
}
