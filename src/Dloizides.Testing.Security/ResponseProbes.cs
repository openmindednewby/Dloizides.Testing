using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Dloizides.Testing.Security;

internal static partial class ResponseProbes
{
    private const string ProblemJson = "application/problem+json";
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";
    private const string Missing = "missing";
    private const string JsonMediaType = "application/json";
    private const string DeniedStatuses = "403 or 404";

    private static readonly RequiredHeader[] RequiredHeaders =
    [
        new("X-Content-Type-Options", "nosniff", "X-Content-Type-Options: nosniff"),
        new("Content-Security-Policy", "frame-ancestors 'none'", "Content-Security-Policy with frame-ancestors 'none'"),
        new("Referrer-Policy", string.Empty, "Referrer-Policy"),
        new("Permissions-Policy", string.Empty, "Permissions-Policy"),
        new("Strict-Transport-Security", string.Empty, "Strict-Transport-Security"),
    ];

    internal static async Task<IReadOnlyList<SecurityProbeFailure>> RunAsync(SecurityProbeTarget target, CancellationToken cancellationToken) =>
    [
        .. await ManagementAsync(target, cancellationToken),
        .. await HeadersAsync(target, cancellationToken),
        .. await ErrorDetailsAsync(target, cancellationToken),
        .. await CorsAsync(target, cancellationToken),
        .. await CrossTenantAsync(target, cancellationToken),
    ];

    private static async Task<IEnumerable<SecurityProbeFailure>> ManagementAsync(SecurityProbeTarget target, CancellationToken cancellationToken)
    {
        var path = target.MetricsPath;
        using var response = await target.GetAsync(path, token: null, cancellationToken);
        return OneOrNone(ProbeRequests.Expect(SecurityProbeKind.ManagementPort, $"GET {path}", HttpStatusCode.NotFound, response));
    }

    private static async Task<IEnumerable<SecurityProbeFailure>> HeadersAsync(SecurityProbeTarget target, CancellationToken cancellationToken)
    {
        var path = target.ProtectedPath;
        using var response = await target.GetAsync(path, target.ValidToken(), cancellationToken);
        var request = $"GET {path} with a valid token";
        return RequiredHeaders
            .Where(header => !HasHeader(response, header))
            .Select(header => new SecurityProbeFailure(SecurityProbeKind.SecurityHeaders, request, header.Description, Missing))
            .ToList();
    }

    private static async Task<IEnumerable<SecurityProbeFailure>> ErrorDetailsAsync(SecurityProbeTarget target, CancellationToken cancellationToken)
    {
        var path = target.ThrowingPath;
        using var response = await target.GetAsync(path, target.ValidToken(), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        var request = $"GET {path}";
        var failures = new List<SecurityProbeFailure?>
        {
            ProbeRequests.Expect(SecurityProbeKind.ErrorDetails, request, HttpStatusCode.InternalServerError, response),
            string.Equals(mediaType, ProblemJson, StringComparison.OrdinalIgnoreCase)
                ? null
                : new SecurityProbeFailure(SecurityProbeKind.ErrorDetails, request, ProblemJson, mediaType ?? "no content type"),
            body.Contains(SecurityProbe.ForcedExceptionMessage, StringComparison.Ordinal)
                ? new SecurityProbeFailure(SecurityProbeKind.ErrorDetails, request, "no exception message", "the exception message")
                : null,
            StackFrame.IsMatch(body)
                ? new SecurityProbeFailure(SecurityProbeKind.ErrorDetails, request, "no stack trace", "a stack frame")
                : null,
        };
        return failures.OfType<SecurityProbeFailure>().ToList();
    }

    private static async Task<IEnumerable<SecurityProbeFailure>> CorsAsync(SecurityProbeTarget target, CancellationToken cancellationToken)
    {
        var path = target.ProtectedPath;
        var origin = target.UnknownOrigin;
        var failures = new List<SecurityProbeFailure>();
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Options })
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", HttpMethod.Get.Method);
            using var response = await target.SendAsync(request, token: null, cancellationToken);
            if (response.Headers.TryGetValues(AllowOriginHeader, out var values))
            {
                failures.Add(new SecurityProbeFailure(
                    SecurityProbeKind.Cors,
                    $"{method.Method} {path} from {origin}",
                    $"no {AllowOriginHeader}",
                    $"{AllowOriginHeader}: {string.Join(", ", values)}"));
            }
        }

        return failures;
    }

    private static async Task<IEnumerable<SecurityProbeFailure>> CrossTenantAsync(SecurityProbeTarget target, CancellationToken cancellationToken)
    {
        var failures = new List<SecurityProbeFailure?>();
        foreach (var entry in target.CrossTenantRequests)
        {
            using var response = await target.SendAsync(ToMessage(entry), target.ValidToken(), cancellationToken);
            failures.Add(ExpectDenied($"{entry.Method.Method} {entry.Path} with a valid token", response));
        }

        return failures.OfType<SecurityProbeFailure>().ToList();
    }

    private static HttpRequestMessage ToMessage(CrossTenantRequest entry) =>
        new(entry.Method, entry.Path)
        {
            Content = entry.JsonBody is null ? null : new StringContent(entry.JsonBody, Encoding.UTF8, JsonMediaType),
        };

    private static SecurityProbeFailure? ExpectDenied(string request, HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound
            ? null
            : new SecurityProbeFailure(SecurityProbeKind.CrossTenant, request, DeniedStatuses, ProbeRequests.StatusOf(response));

    private static bool HasHeader(HttpResponseMessage response, RequiredHeader header) =>
        (response.Headers.TryGetValues(header.Name, out var values)
            || response.Content.Headers.TryGetValues(header.Name, out values))
        && values.Any(value => value.Contains(header.MustContain, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<SecurityProbeFailure> OneOrNone(SecurityProbeFailure? failure) =>
        failure is null ? [] : [failure];

    [GeneratedRegex(@"\bat [\w.`<>+]+\(", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex StackFrame { get; }

    private sealed record RequiredHeader(string Name, string MustContain, string Description);
}
