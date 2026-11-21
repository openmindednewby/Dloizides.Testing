namespace Dloizides.Testing.Security;

/// <summary>One control the service under test is missing.</summary>
public sealed record SecurityProbeFailure(SecurityProbeKind Kind, string Request, string Expected, string Actual)
{
    /// <summary>The failure as one readable line.</summary>
    public override string ToString() => $"{Kind}: {Request}: expected {Expected}, got {Actual}";
}
