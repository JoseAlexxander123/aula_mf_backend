using AulaVirtual.Modules.Auth.Application.Abstractions;

namespace AulaVirtual.Modules.Auth.Application.Authentication.Logout;

public sealed class LogoutCurrentSessionHandler(
    IAuthRepository repository,
    IRefreshTokenService refreshTokenService,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(
        string? refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = refreshTokenService.ComputeHash(refreshToken);
        var context = await repository.FindRefreshTokenAsync(
            tokenHash,
            cancellationToken);

        if (context is null)
        {
            return;
        }

        context.Session.Revoke(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
    }
}
