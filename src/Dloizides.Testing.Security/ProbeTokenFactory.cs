namespace Dloizides.Testing.Security;

/// <summary>Creates a signed token the service under test accepts, for the given audience and expiry.</summary>
public delegate string ProbeTokenFactory(string audience, DateTimeOffset expires);
