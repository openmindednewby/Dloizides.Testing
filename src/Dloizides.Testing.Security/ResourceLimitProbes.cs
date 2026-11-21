using System.Globalization;
using System.Net;

namespace Dloizides.Testing.Security;

internal static class ResourceLimitProbes
{
    internal static async Task<IReadOnlyList<SecurityProbeFailure>> RunAsync(SecurityProbeTarget target, CancellationToken cancellationToken) =>
    [
        .. await BodyLimitAsync(target, cancellationToken),
        .. await BurstAsync(target, cancellationToken),
    ];

    private static async Task<IEnumerable<SecurityProbeFailure>> BodyLimitAsync(SecurityProbeTarget target, CancellationToken cancellationToken)
    {
        var path = target.BodyPath;
        var size = target.MaxRequestBodyBytes + 1;
        var description = $"POST {path} with {size.ToString(CultureInfo.InvariantCulture)} bytes";
        var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new ByteArrayContent(new byte[size]) };
        try
        {
            using var response = await target.SendAsync(request, target.ValidToken(), cancellationToken);
            var failure = ProbeRequests.Expect(SecurityProbeKind.BodyLimit, description, HttpStatusCode.RequestEntityTooLarge, response);
            return failure is null ? [] : [failure];
        }
        catch (HttpRequestException exception)
        {
            return [new SecurityProbeFailure(SecurityProbeKind.BodyLimit, description, "413", $"no response ({exception.Message})")];
        }
    }

    private static async Task<IEnumerable<SecurityProbeFailure>> BurstAsync(SecurityProbeTarget target, CancellationToken cancellationToken)
    {
        var path = target.ProtectedPath;
        var count = target.BurstRequests;
        for (var sent = 0; sent < count; sent++)
        {
            using var response = await target.GetAsync(path, token: null, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return [];
        }

        var description = $"{count.ToString(CultureInfo.InvariantCulture)} x GET {path} without a token";
        return [new SecurityProbeFailure(SecurityProbeKind.RateLimit, description, "at least one 429", "none")];
    }
}
