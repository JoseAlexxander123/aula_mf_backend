using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Domain.Accounts;

namespace AulaVirtual.Modules.Auth.Application.Authentication.Refresh;

public sealed class RefreshSessionHandler(
    IAuthRepository repository,
    IRefreshTokenService refreshTokenService,
    IAccessTokenService accessTokenService,
    TimeProvider timeProvider,
    AuthenticationSettings settings)
{
    public async Task<AuthenticationResult> ExecuteAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new AuthenticationResult(
                AuthenticationStatus.InvalidSession);
        }

        var tokenHash = refreshTokenService.ComputeHash(refreshToken);
        var context = await repository.FindRefreshTokenAsync(
            tokenHash,
            cancellationToken);

        if (context is null)
        {
            return new AuthenticationResult(
                AuthenticationStatus.InvalidSession);
        }

        var now = timeProvider.GetUtcNow();

        if (context.Token.WasReused)
        {
            context.Session.Revoke(now);
            await repository.SaveChangesAsync(cancellationToken);

            return new AuthenticationResult(
                AuthenticationStatus.ReusedRefreshToken);
        }

        if (context.Account.Status != AccountStatus.Active ||
            !context.Session.IsActive(now) ||
            !context.Token.IsActive(now))
        {
            return new AuthenticationResult(
                AuthenticationStatus.InvalidSession);
        }

        var replacementValue = refreshTokenService.Generate();
        var replacementHash =
            refreshTokenService.ComputeHash(replacementValue);
        var replacementId = Guid.NewGuid();
        var replacementExpiration =
            now.AddDays(settings.RefreshTokenDays);

        context.Token.MarkUsed(now, replacementId);
        context.Session.Touch(now, replacementExpiration);
        context.Session.AddRefreshToken(
            replacementId,
            replacementHash,
            now,
            replacementExpiration);

        var accessToken = accessTokenService.Generate(context.Account);

        await repository.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            AuthenticationStatus.Succeeded,
            accessToken.Value,
            accessToken.TokenType,
            accessToken.ExpiresIn,
            replacementValue);
    }
}
