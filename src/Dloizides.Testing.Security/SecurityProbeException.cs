namespace Dloizides.Testing.Security;

/// <summary>Thrown when the service under test is missing one or more security controls.</summary>
public sealed class SecurityProbeException : Exception
{
    /// <summary>Creates the exception with one message line per failed probe.</summary>
    public SecurityProbeException(IReadOnlyList<SecurityProbeFailure> failures)
        : base("Security probe failed:" + Environment.NewLine + string.Join(Environment.NewLine, failures))
    {
        Failures = failures;
    }

    /// <summary>The failed probes.</summary>
    public IReadOnlyList<SecurityProbeFailure> Failures { get; }
}
