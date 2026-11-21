using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dloizides.Testing.Security.Tests;

internal sealed class ProbeHost : IAsyncDisposable
{
    public const string ItemsPath = "/items";
    public const string FailPath = "/items/fail";
    public const string OtherTenantPath = "/tenants/other/items/1";
    public const string Audience = "probe-tests-api";
    public const long BodyLimit = 4096;

    private readonly WebApplication _app;

    private readonly FakeIssuer _issuer;

    private ProbeHost(WebApplication app, FakeIssuer issuer, X509Certificate2 certificate)
    {
        _app = app;
        _issuer = issuer;
        var address = app.Services.GetRequiredService<IServer>().Features
            .GetRequiredFeature<IServerAddressesFeature>().Addresses.First();
        var trust = new X509ChainPolicy { TrustMode = X509ChainTrustMode.CustomRootTrust };
        trust.CustomTrustStore.Add(certificate);
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions { CertificateChainPolicy = trust },
        };
        Client = new HttpClient(handler) { BaseAddress = new Uri(address) };
    }

    public HttpClient Client { get; }

    public SecurityProbeTarget Target => new()
    {
        Client = Client,
        Audience = Audience,
        CreateToken = _issuer.CreateToken,
        ProtectedPath = ItemsPath,
        ThrowingPath = FailPath,
        BodyPath = ItemsPath,
        MaxRequestBodyBytes = BodyLimit,
        CrossTenantPaths = [OtherTenantPath],
    };

    public static async Task<ProbeHost> StartLeakyAsync()
    {
        var issuer = await FakeIssuer.StartAsync();
        var certificate = CreateCertificate();
        var builder = CreateBuilder(certificate);
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = null);
        builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin()));
        var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.UseCors();
        MapEndpoints(app, Results.Ok("another tenant's item"));
        app.MapGet("/metrics", () => "probe_metric 1");
        return await StartAsync(app, issuer, certificate);
    }

    public static async Task<ProbeHost> StartServiceDefaultsAsync()
    {
        var issuer = await FakeIssuer.StartAsync();
        var certificate = CreateCertificate();
        var builder = CreateBuilder(certificate);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [$"{AllowedOriginsCorsOptions.SectionName}:0"] = "https://allowed.example",
            ["ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/8",
            ["Jwt:Authority"] = issuer.Authority,
            ["Jwt:Audience"] = Audience,
            ["Jwt:RequireHttpsMetadata"] = "false",
            ["Authentication:AllowedRealms:0"] = FakeIssuer.Realm,
        });
        builder.AddDloizidesServiceDefaults(options =>
        {
            options.ServiceName = "security-probe-tests";
            options.MaxRequestBodyBytes = BodyLimit;
            options.Secrets.None("probe test host holds no secrets");
        });
        var app = builder.Build();
        app.UseDloizidesServiceDefaults();
        MapEndpoints(app, Results.NotFound());
        return await StartAsync(app, issuer, certificate);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
        await _issuer.DisposeAsync();
    }

    private static WebApplicationBuilder CreateBuilder(X509Certificate2 certificate)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(certificate)));
        return builder;
    }

    private static void MapEndpoints(WebApplication app, IResult otherTenantItem)
    {
        app.MapGet(ItemsPath, () => "items");
        app.MapGet(FailPath, string () => throw new InvalidOperationException(SecurityProbe.ForcedExceptionMessage));
        app.MapPost(ItemsPath, async (HttpRequest request) =>
        {
            await request.Body.CopyToAsync(Stream.Null, request.HttpContext.RequestAborted);
            return Results.Ok();
        });
        app.MapGet(OtherTenantPath, () => otherTenantItem);
    }

    private static async Task<ProbeHost> StartAsync(WebApplication app, FakeIssuer issuer, X509Certificate2 certificate)
    {
        await app.StartAsync(CancellationToken.None);
        return new ProbeHost(app, issuer, certificate);
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddIpAddress(IPAddress.Loopback);
        names.AddDnsName("localhost");
        request.CertificateExtensions.Add(names.Build());
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), password: null);
    }
}
