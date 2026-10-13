using AulaVirtual.Modules.Auth.Domain.Accounts;
using AulaVirtual.Modules.Auth.Domain.Sessions;

namespace AulaVirtual.Modules.Auth.Application.Abstractions;

public interface IAuthRepository
{
    Task<AuthAccount?> FindAccountByIdentifierAsync(
        LoginType loginType,
        string normalizedLoginIdentifier,
        CancellationToken cancellationToken);

    Task<AuthAccount?> FindAccountByIdAsync(
        Guid authAccountId,
        CancellationToken cancellationToken);

    Task<RefreshTokenContext?> FindRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AuthSession>> GetSessionsAsync(
        Guid authAccountId,
        CancellationToken cancellationToken);

    void AddAccount(AuthAccount account);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record RefreshTokenContext(
    RefreshToken Token,
    AuthSession Session,
    AuthAccount Account);
