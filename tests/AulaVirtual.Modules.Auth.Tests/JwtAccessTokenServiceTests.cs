using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using AulaVirtual.Modules.Auth.Application.Authentication;
using AulaVirtual.Modules.Auth.Domain.Accounts;
using AulaVirtual.Modules.Auth.Infrastructure.Security;

namespace AulaVirtual.Modules.Auth.Tests;

public sealed class JwtAccessTokenServiceTests
{
    private static readonly string SigningKey = Convert.ToBase64String(
        RandomNumberGenerator.GetBytes(32));

    [Fact]
    public void Generate_EmitsConfirmedHs256TokenAndClaims()
    {
        var now = new DateTimeOffset(
            2026,
            9,
            25,
            12,
            0,
            0,
            TimeSpan.Zero);
        var accountId = Guid.Parse(
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var userId = Guid.Parse(
            "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var account = AuthAccount.Create(
            accountId,
            userId,
            "encoded",
            now);
        account.AddIdentifier(
            Guid.NewGuid(),
            LoginType.Username,
            "G77340061",
            now);
        var service = new JwtAccessTokenService(
            new JwtSettings
            {
                Issuer = "AulaVirtual.Api",
                Audience = "AulaVirtual.Web",
                SigningKey = SigningKey
            },
            new AuthenticationSettings
            {
                AccessTokenMinutes = 15
            },
            new MutableTimeProvider(now));

        var accessToken = service.Generate(account);
        var token = new JwtSecurityTokenHandler()
            .ReadJwtToken(accessToken.Value);

        Assert.Equal("Bearer", accessToken.TokenType);
        Assert.Equal(900, accessToken.ExpiresIn);
        Assert.Equal("HS256", token.Header.Alg);
        Assert.Equal("AulaVirtual.Api", token.Issuer);
        Assert.Equal("AulaVirtual.Web", Assert.Single(token.Audiences));
        Assert.Equal(
            accountId.ToString(),
            token.Claims.Single(
                claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(
            userId.ToString(),
            token.Claims.Single(
                claim => claim.Type == "user_id").Value);
        Assert.True(Guid.TryParseExact(
            token.Claims.Single(
                claim => claim.Type == JwtRegisteredClaimNames.Jti).Value,
            "N",
            out _));
        Assert.Equal(
            now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            token.Claims.Single(
                claim => claim.Type == JwtRegisteredClaimNames.Iat).Value);
        Assert.Equal(now.AddMinutes(15).UtcDateTime, token.ValidTo);
        Assert.DoesNotContain(
            token.Claims,
            claim => claim.Type.Contains(
                "role",
                StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(token.Claims, claim =>
            claim.Type.Contains("permission", StringComparison.OrdinalIgnoreCase) ||
            claim.Type.Contains("session", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Settings_RejectSigningKeysShorterThan256Bits()
    {
        var settings = new JwtSettings
        {
            SigningKey = "too-short"
        };

        var exception = Assert.Throws<InvalidOperationException>(
            settings.GetSigningKeyBytes);

        Assert.Contains("256 bits", exception.Message);
    }
}
