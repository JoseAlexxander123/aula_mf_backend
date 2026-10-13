using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Domain.Accounts;
using AulaVirtual.Modules.Auth.Domain.Sessions;

namespace AulaVirtual.Modules.Auth.Tests;

internal class FakeAuthRepository : IAuthRepository
{
    public List<AuthAccount> Accounts { get; } = [];

    public int SaveChangesCalls { get; private set; }

    public Task<AuthAccount?> FindAccountByIdentifierAsync(
        LoginType loginType,
        string normalizedLoginIdentifier,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Accounts.SingleOrDefault(
            account =>
                account.Identifiers.Any(identifier =>
                    identifier.LoginType == loginType &&
                    identifier.NormalizedLoginIdentifier ==
                        normalizedLoginIdentifier)));
    }

    public Task<AuthAccount?> FindAccountByIdAsync(
        Guid authAccountId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Accounts.SingleOrDefault(
            account => account.Id == authAccountId));
    }

    public Task<RefreshTokenContext?> FindRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        foreach (var account in Accounts)
        {
            foreach (var session in account.Sessions)
            {
                var token = session.RefreshTokens.SingleOrDefault(
                    candidate => candidate.TokenHash == tokenHash);

                if (token is not null)
                {
                    return Task.FromResult<RefreshTokenContext?>(
                        new RefreshTokenContext(token, session, account));
                }
            }
        }

        return Task.FromResult<RefreshTokenContext?>(null);
    }

    public Task<IReadOnlyList<AuthSession>> GetSessionsAsync(
        Guid authAccountId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AuthSession> sessions = Accounts
            .Single(account => account.Id == authAccountId)
            .Sessions
            .ToList();

        return Task.FromResult(sessions);
    }

    public virtual void AddAccount(AuthAccount account)
    {
        Accounts.Add(account);
    }

    public virtual Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCalls++;
        return Task.CompletedTask;
    }
}

internal sealed class StubPasswordHasher(string validPassword)
    : IPasswordHasher
{
    public string Hash(string password) => "encoded";

    public bool Verify(string password, string encodedHash)
    {
        return password == validPassword;
    }
}

internal sealed class StubAccessTokenService : IAccessTokenService
{
    public AccessToken Generate(AuthAccount account)
    {
        return new AccessToken("access-token", "Bearer", 900);
    }
}

internal sealed class SequentialRefreshTokenService : IRefreshTokenService
{
    private int sequence = 1;

    public string Generate()
    {
        sequence++;
        return "refresh-token-" + sequence;
    }

    public string ComputeHash(string token)
    {
        return "hash:" + token;
    }
}

internal sealed class MutableTimeProvider(DateTimeOffset utcNow)
    : TimeProvider
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;

    public void Advance(TimeSpan duration)
    {
        UtcNow = UtcNow.Add(duration);
    }
}
