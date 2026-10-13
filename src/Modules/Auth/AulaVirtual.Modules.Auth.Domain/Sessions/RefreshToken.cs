namespace AulaVirtual.Modules.Auth.Domain.Sessions;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid sessionId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        SessionId = sessionId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActive(DateTimeOffset now)
    {
        return UsedAt is null &&
            RevokedAt is null &&
            ExpiresAt > now;
    }

    public bool WasReused => UsedAt is not null || ReplacedByTokenId is not null;

    public static RefreshToken Create(
        Guid id,
        Guid sessionId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        if (sessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "A session identifier is required.",
                nameof(sessionId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAt <= createdAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "The refresh token expiration must be after its creation.");
        }

        return new RefreshToken(
            id,
            sessionId,
            tokenHash,
            createdAt,
            expiresAt);
    }

    public void MarkUsed(DateTimeOffset now, Guid replacementTokenId)
    {
        if (!IsActive(now))
        {
            throw new InvalidOperationException("The refresh token is not active.");
        }

        UsedAt = now;
        ReplacedByTokenId = replacementTokenId;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAt ??= now;
    }
}
