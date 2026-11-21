using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Dloizides.Testing.Security;

internal static class ProbeTokens
{
    private const string UnsignedHeader = """{"alg":"none","typ":"JWT"}""";

    private const int ForgedSignatureBytes = 256;

    private const int SignedTokenParts = 3;

    internal static string Forge(string token)
    {
        var parts = Split(token);
        var signature = RandomNumberGenerator.GetBytes(ForgedSignatureBytes);
        return $"{parts.Header}.{parts.Payload}.{Base64Url.EncodeToString(signature)}";
    }

    internal static string Unsigned(string token)
    {
        var header = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(UnsignedHeader));
        return $"{header}.{Split(token).Payload}.";
    }

    private static TokenParts Split(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != SignedTokenParts)
            throw new ArgumentException("The token factory must return a signed JWT (header.payload.signature).", nameof(token));
        return new TokenParts(parts[0], parts[1]);
    }

    private sealed record TokenParts(string Header, string Payload);
}
