using System.Text;

namespace AulaVirtual.Modules.Auth.Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; init; } = "AulaVirtual.Api";

    public string Audience { get; init; } = "AulaVirtual.Web";

    public string SigningKey { get; init; } = string.Empty;

    public byte[] GetSigningKeyBytes()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(Audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(SigningKey);

        var signingKeyBytes = Encoding.UTF8.GetBytes(SigningKey);

        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Authentication:Jwt:SigningKey must contain at least 256 bits and must be supplied externally.");
        }

        return signingKeyBytes;
    }
}
