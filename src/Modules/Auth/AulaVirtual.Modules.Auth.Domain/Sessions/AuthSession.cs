namespace AulaVirtual.Modules.Auth.Domain.Sessions;

public sealed class AuthSession
{
    private readonly List<RefreshToken> refreshTokens = [];

    private AuthSession()
    {
    }

    private AuthSession(
        Guid id,
        Guid authAccountId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        AuthAccountId = authAccountId;
        CreatedAt = createdAt;
        LastUsedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid AuthAccountId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastUsedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => refreshTokens;

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null && ExpiresAt > now;
    }

    public static AuthSession Create(
        Guid id,
        Guid authAccountId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        if (authAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "An authentication account identifier is required.",
                nameof(authAccountId));
        }

        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "The session expiration must be after its creation.");
        }

        return new AuthSession(id, authAccountId, createdAt, expiresAt);
    }

    public RefreshToken AddRefreshToken(
        Guid tokenId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        if (!IsActive(createdAt))
        {
            throw new InvalidOperationException(
                "A refresh token cannot be added to an inactive session.");
        }

        var token = RefreshToken.Create(
            tokenId,
            Id,
            tokenHash,
            createdAt,
            expiresAt);

        refreshTokens.Add(token);
        return token;
    }

    public void Touch(DateTimeOffset now, DateTimeOffset expiresAt)
    {
        LastUsedAt = now;
        ExpiresAt = expiresAt;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;

        foreach (var token in refreshTokens)
        {
            token.Revoke(now);
        }
    }
}
