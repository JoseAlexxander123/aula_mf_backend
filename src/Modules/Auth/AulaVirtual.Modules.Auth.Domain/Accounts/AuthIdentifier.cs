namespace AulaVirtual.Modules.Auth.Domain.Accounts;

public sealed class AuthIdentifier
{
    private AuthIdentifier()
    {
    }

    private AuthIdentifier(
        Guid id,
        Guid authAccountId,
        LoginType loginType,
        string loginIdentifier,
        string normalizedLoginIdentifier,
        DateTimeOffset createdAt)
    {
        Id = id;
        AuthAccountId = authAccountId;
        LoginType = loginType;
        LoginIdentifier = loginIdentifier;
        NormalizedLoginIdentifier = normalizedLoginIdentifier;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AuthAccountId { get; private set; }

    public LoginType LoginType { get; private set; }

    public string LoginIdentifier { get; private set; } = string.Empty;

    public string NormalizedLoginIdentifier { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    internal static AuthIdentifier Create(
        Guid id,
        Guid authAccountId,
        LoginType loginType,
        string loginIdentifier,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "An identifier ID is required.",
                nameof(id));
        }

        if (authAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "An auth account ID is required.",
                nameof(authAccountId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(loginIdentifier);

        var trimmedIdentifier = loginIdentifier.Trim();

        return new AuthIdentifier(
            id,
            authAccountId,
            loginType,
            trimmedIdentifier,
            trimmedIdentifier.ToUpperInvariant(),
            createdAt);
    }
}
