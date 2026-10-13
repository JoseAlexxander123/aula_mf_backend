using System.Security.Cryptography;
using System.Text;
using AulaVirtual.Modules.Auth.Application.Abstractions;

namespace AulaVirtual.Modules.Auth.Infrastructure.Security;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string ComputeHash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
