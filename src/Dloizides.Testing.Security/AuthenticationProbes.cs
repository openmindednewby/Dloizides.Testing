using System.Net;

namespace Dloizides.Testing.Security;

internal static class AuthenticationProbes
{
    internal const string WrongAudience = "security-probe-wrong-audience";

    private static readonly TimeSpan PastExpiry = TimeSpan.FromMinutes(-10);

    internal static async Task<IReadOnlyList<SecurityProbeFailure>> RunAsync(SecurityProbeTarget target, CancellationToken cancellationToken)
    {
        var path = target.ProtectedPath;
        var valid = target.ValidToken();
        var failures = new List<SecurityProbeFailure?>();
        using (var accepted = await target.GetAsync(path, valid, cancellationToken))
        {
            if (accepted.StatusCode == HttpStatusCode.Unauthorized)
                failures.Add(new SecurityProbeFailure(SecurityProbeKind.JwtValidation, $"GET {path} with a valid token", "not 401", "401"));
        }

        using (var anonymous = await target.GetAsync(path, token: null, cancellationToken))
        {
            failures.Add(ProbeRequests.Expect(SecurityProbeKind.FallbackPolicy, $"GET {path} without a token", HttpStatusCode.Unauthorized, anonymous));
        }

        foreach (var badToken in BadTokens(target, valid))
        {
            using var response = await target.GetAsync(path, badToken.Token, cancellationToken);
            failures.Add(ProbeRequests.Expect(SecurityProbeKind.JwtValidation, $"GET {path} with {badToken.Label}", HttpStatusCode.Unauthorized, response));
        }

        return failures.OfType<SecurityProbeFailure>().ToList();
    }

    private static IEnumerable<BadToken> BadTokens(SecurityProbeTarget target, string valid) =>
    [
        new BadToken("a forged signature", ProbeTokens.Forge(valid)),
        new BadToken("an expired token", target.TokenExpiringIn(target.Audience, PastExpiry)),
        new BadToken("a wrong audience", target.TokenExpiringIn(WrongAudience, ProbeRequests.ValidLifetime)),
        new BadToken("alg none", ProbeTokens.Unsigned(valid)),
    ];

    private sealed record BadToken(string Label, string Token);
}
