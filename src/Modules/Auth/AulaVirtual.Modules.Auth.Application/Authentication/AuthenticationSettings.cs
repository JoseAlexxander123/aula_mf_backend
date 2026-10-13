namespace AulaVirtual.Modules.Auth.Application.Authentication;

public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public int AccessTokenMinutes { get; init; } = 15;

    public int RefreshTokenDays { get; init; } = 7;

    public int MaximumFailedLoginAttempts { get; init; } = 5;

    public int LockoutMinutes { get; init; } = 10;
}
