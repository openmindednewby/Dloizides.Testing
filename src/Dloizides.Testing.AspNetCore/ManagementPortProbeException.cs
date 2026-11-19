namespace Dloizides.Testing.AspNetCore;

/// <summary>Thrown when a management path is reachable on the public port or missing on the management port.</summary>
public sealed class ManagementPortProbeException : Exception
{
    /// <summary>Creates the exception with one message line per failed probe.</summary>
    public ManagementPortProbeException(IReadOnlyList<ManagementPortProbeFailure> failures)
        : base("MGMT-PORT-1 probe failed:" + Environment.NewLine + string.Join(Environment.NewLine, failures))
    {
        Failures = failures;
    }

    /// <summary>The failed probes.</summary>
    public IReadOnlyList<ManagementPortProbeFailure> Failures { get; }
}
