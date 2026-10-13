using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Domain.Accounts;
using AulaVirtual.Modules.Auth.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Auth.Infrastructure.Persistence;

public sealed class AuthRepository(AuthDbContext dbContext) : IAuthRepository
{
    public async Task<AuthAccount?> FindAccountByIdentifierAsync(
        LoginType loginType,
        string normalizedLoginIdentifier,
        CancellationToken cancellationToken)
    {
        var authAccountId = await dbContext.AuthIdentifiers
            .AsNoTracking()
            .Where(identifier =>
                identifier.LoginType == loginType &&
                identifier.NormalizedLoginIdentifier ==
                    normalizedLoginIdentifier)
            .Select(identifier => (Guid?)identifier.AuthAccountId)
            .SingleOrDefaultAsync(cancellationToken);

        if (authAccountId is null)
        {
            return null;
        }

        return await dbContext.AuthAccounts
            .Include(account => account.Identifiers)
            .Include(account => account.Sessions)
            .ThenInclude(session => session.RefreshTokens)
            .SingleOrDefaultAsync(
                account => account.Id == authAccountId.Value,
                cancellationToken);
    }

    public Task<AuthAccount?> FindAccountByIdAsync(
        Guid authAccountId,
        CancellationToken cancellationToken)
    {
        return dbContext.AuthAccounts
            .Include(account => account.Identifiers)
            .SingleOrDefaultAsync(
                account => account.Id == authAccountId,
                cancellationToken);
    }

    public async Task<RefreshTokenContext?> FindRefreshTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            candidate => candidate.TokenHash == tokenHash,
            cancellationToken);

        if (token is null)
        {
            return null;
        }

        var session = await dbContext.AuthSessions
            .Include(candidate => candidate.RefreshTokens)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == token.SessionId,
                cancellationToken);

        if (session is null)
        {
            return null;
        }

        var account = await dbContext.AuthAccounts.SingleOrDefaultAsync(
            candidate => candidate.Id == session.AuthAccountId,
            cancellationToken);

        return account is null
            ? null
            : new RefreshTokenContext(token, session, account);
    }

    public async Task<IReadOnlyList<AuthSession>> GetSessionsAsync(
        Guid authAccountId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AuthSessions
            .Include(session => session.RefreshTokens)
            .Where(session => session.AuthAccountId == authAccountId)
            .ToListAsync(cancellationToken);
    }

    public void AddAccount(AuthAccount account)
    {
        dbContext.AuthAccounts.Add(account);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
