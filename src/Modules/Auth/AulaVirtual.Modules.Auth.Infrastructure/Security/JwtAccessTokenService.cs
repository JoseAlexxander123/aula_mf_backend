using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Application.Authentication;
using AulaVirtual.Modules.Auth.Domain.Accounts;
using Microsoft.IdentityModel.Tokens;

namespace AulaVirtual.Modules.Auth.Infrastructure.Security;

public sealed class JwtAccessTokenService(
    JwtSettings jwtSettings,
    AuthenticationSettings authenticationSettings,
    TimeProvider timeProvider) : IAccessTokenService
{
    private readonly byte[] signingKey = jwtSettings.GetSigningKeyBytes();

    public AccessToken Generate(AuthAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (authenticationSettings.AccessTokenMinutes <= 0)
        {
            throw new InvalidOperationException(
                "Authentication:AccessTokenMinutes must be greater than zero.");
        }

        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(
            authenticationSettings.AccessTokenMinutes);
        var expiresIn = checked(
            authenticationSettings.AccessTokenMinutes * 60);
        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                account.Id.ToString()),
            new Claim("user_id", account.UserId.ToString()),
            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N")),
            new Claim(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(
                    CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(signingKey),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            notBefore: null,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new AccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            expiresIn);
    }
}
