using AulaVirtual.Modules.Auth.Domain.Sessions;

namespace AulaVirtual.Modules.Auth.Domain.Accounts;

public sealed class AuthAccount
{
    private readonly List<AuthIdentifier> identifiers = [];
    private readonly List<AuthSession> sessions = [];

    private AuthAccount()
    {
    }

    private AuthAccount(
        Guid id,
        Guid userId,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        PasswordHash = passwordHash;
        Status = AccountStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public AccountStatus Status { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockedUntil { get; private set; }

    public DateTimeOffset? PasswordChangedAt { get; private set; }

    public DateTimeOffset? LastLoginAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<AuthIdentifier> Identifiers => identifiers;

    public IReadOnlyCollection<AuthSession> Sessions => sessions;

    public bool IsLocked(DateTimeOffset now)
    {
        return LockedUntil is not null && LockedUntil > now;
    }

    public static AuthAccount Create(
        Guid id,
        Guid userId,
        string passwordHash,
        DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user identifier is required.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new AuthAccount(
            id,
            userId,
            passwordHash,
            createdAt);
    }

    public AuthIdentifier AddIdentifier(
        Guid identifierId,
        LoginType loginType,
        string loginIdentifier,
        DateTimeOffset createdAt)
    {
        if (identifiers.Any(identifier => identifier.LoginType == loginType))
        {
            throw new InvalidOperationException(
                "An account cannot have more than one identifier of the same type.");
        }

        var identifier = AuthIdentifier.Create(
            identifierId,
            Id,
            loginType,
            loginIdentifier,
            createdAt);
        identifiers.Add(identifier);

        return identifier;
    }

    public void RegisterFailedLogin(
        DateTimeOffset now,
        int maximumAttempts,
        TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= maximumAttempts)
        {
            LockedUntil = now.Add(lockoutDuration);
        }

        UpdatedAt = now;
    }

    public void RegisterSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        LastLoginAt = now;
        UpdatedAt = now;
    }

    public AuthSession StartSession(
        Guid sessionId,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        if (Status != AccountStatus.Active)
        {
            throw new InvalidOperationException("An inactive account cannot start a session.");
        }

        var session = AuthSession.Create(sessionId, Id, now, expiresAt);
        sessions.Add(session);
        return session;
    }

    public void Deactivate(DateTimeOffset now)
    {
        Status = AccountStatus.Inactive;
        UpdatedAt = now;

        foreach (var session in sessions)
        {
            session.Revoke(now);
        }
    }

    public void Activate(DateTimeOffset now)
    {
        Status = AccountStatus.Active;
        UpdatedAt = now;
    }
}
