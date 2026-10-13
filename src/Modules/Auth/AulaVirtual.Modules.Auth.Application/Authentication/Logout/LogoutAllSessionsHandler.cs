using AulaVirtual.Modules.Auth.Application.Abstractions;

namespace AulaVirtual.Modules.Auth.Application.Authentication.Logout;

public sealed class LogoutAllSessionsHandler(
    IAuthRepository repository,
    TimeProvider timeProvider)
{
    public async Task ExecuteAsync(
        Guid authAccountId,
        CancellationToken cancellationToken)
    {
        var sessions = await repository.GetSessionsAsync(
            authAccountId,
            cancellationToken);
        var now = timeProvider.GetUtcNow();

        foreach (var session in sessions)
        {
            session.Revoke(now);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
