using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Dloizides.Testing.Security.Tests;

internal sealed class FakeIssuer : IAsyncDisposable
{
    public const string Realm = "probe";

    private const string KeyId = "probe-key";

    private static readonly TimeSpan IssuedBeforeExpiry = TimeSpan.FromMinutes(10);

    private readonly WebApplication _app;

    private readonly RsaSecurityKey _key;

    private FakeIssuer(WebApplication app, RsaSecurityKey key, string serverUrl)
    {
        _app = app;
        _key = key;
        Authority = $"{serverUrl}/realms/{Realm}";
    }

    public string Authority { get; }

    public static async Task<FakeIssuer> StartAsync()
    {
        var key = new RsaSecurityKey(RSA.Create(2048)) { KeyId = KeyId };
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.MapGet($"/realms/{Realm}/.well-known/openid-configuration", (HttpContext context) => Results.Json(new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["issuer"] = IssuerOf(context),
            ["jwks_uri"] = $"{IssuerOf(context)}/protocol/openid-connect/certs",
            ["id_token_signing_alg_values_supported"] = new[] { SecurityAlgorithms.RsaSha256 },
        }));
        app.MapGet($"/realms/{Realm}/protocol/openid-connect/certs", () => Results.Json(new { keys = new[] { PublicJwk(key) } }));
        await app.StartAsync(CancellationToken.None);

        var address = app.Services.GetRequiredService<IServer>().Features
            .GetRequiredFeature<IServerAddressesFeature>().Addresses.First();
        return new FakeIssuer(app, key, address);
    }

    public string CreateToken(string audience, DateTimeOffset expires) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Authority,
            Audience = audience,
            IssuedAt = expires.UtcDateTime - IssuedBeforeExpiry,
            NotBefore = expires.UtcDateTime - IssuedBeforeExpiry,
            Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity([new Claim("sub", "probe-user")]),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.RsaSha256),
        });

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    private static string IssuerOf(HttpContext context) => $"{context.Request.Scheme}://{context.Request.Host}/realms/{Realm}";

    private static Dictionary<string, string> PublicJwk(RsaSecurityKey key)
    {
        var parameters = key.Rsa.ExportParameters(includePrivateParameters: false);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["kty"] = "RSA",
            ["use"] = "sig",
            ["alg"] = SecurityAlgorithms.RsaSha256,
            ["kid"] = KeyId,
            ["n"] = Base64UrlEncoder.Encode(parameters.Modulus),
            ["e"] = Base64UrlEncoder.Encode(parameters.Exponent),
        };
    }
}
