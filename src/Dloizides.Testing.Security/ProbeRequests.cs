using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace Dloizides.Testing.Security;

internal static class ProbeRequests
{
    internal static readonly TimeSpan ValidLifetime = TimeSpan.FromMinutes(5);

    private const string BearerScheme = "Bearer";

    internal static string StatusOf(HttpResponseMessage response) =>
        ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);

    internal static SecurityProbeFailure? Expect(
        SecurityProbeKind kind,
        string request,
        HttpStatusCode expected,
        HttpResponseMessage response) =>
        response.StatusCode == expected
            ? null
            : new SecurityProbeFailure(kind, request, ((int)expected).ToString(CultureInfo.InvariantCulture), StatusOf(response));

    internal static string TokenExpiringIn(this SecurityProbeTarget target, string audience, TimeSpan expiresIn) =>
        target.CreateToken(audience, DateTimeOffset.UtcNow.Add(expiresIn));

    internal static string ValidToken(this SecurityProbeTarget target) => target.TokenExpiringIn(target.Audience, ValidLifetime);

    internal static Task<HttpResponseMessage> GetAsync(
        this SecurityProbeTarget target,
        string path, string? token, CancellationToken cancellationToken) =>
        target.SendAsync(new HttpRequestMessage(HttpMethod.Get, path), token, cancellationToken);

    internal static async Task<HttpResponseMessage> SendAsync(
        this SecurityProbeTarget target,
        HttpRequestMessage request, string? token, CancellationToken cancellationToken)
    {
        using (request)
        {
            if (token is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue(BearerScheme, token);
            return await target.Client.SendAsync(request, cancellationToken);
        }
    }
}
