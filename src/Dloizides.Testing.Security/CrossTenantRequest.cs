namespace Dloizides.Testing.Security;

/// <summary>A request to another tenant's record, sent with a valid token; it must answer 403 or 404.</summary>
public sealed record CrossTenantRequest(HttpMethod Method, string Path, string? JsonBody = null);
