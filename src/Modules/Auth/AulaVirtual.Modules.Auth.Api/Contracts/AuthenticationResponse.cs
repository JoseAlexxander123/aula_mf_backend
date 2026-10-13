namespace AulaVirtual.Modules.Auth.Api.Contracts;

public sealed record AuthenticationResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn);
