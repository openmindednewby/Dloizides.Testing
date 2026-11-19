using System.Net;

namespace Dloizides.Testing.AspNetCore;

/// <summary>A management path that answered the wrong status on one port.</summary>
public sealed record ManagementPortProbeFailure(string Path, int Port, HttpStatusCode Expected, HttpStatusCode Actual)
{
    /// <summary>The failure as one readable line.</summary>
    public override string ToString() => $"GET {Path} on port {Port}: expected {(int)Expected}, got {(int)Actual}";
}
