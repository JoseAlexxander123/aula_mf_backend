namespace AulaVirtual.Modules.Auth.Application.Authentication;

public enum AuthenticationStatus
{
    Succeeded,
    InvalidRequest,
    InvalidCredentials,
    Locked,
    InvalidSession,
    ReusedRefreshToken
}

public sealed record AuthenticationResult(
    AuthenticationStatus Status,
    string? AccessToken = null,
    string? TokenType = null,
    int? ExpiresIn = null,
    string? RefreshToken = null);
