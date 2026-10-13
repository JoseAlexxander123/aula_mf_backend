namespace AulaVirtual.Modules.Auth.Api.Contracts;

public sealed record StudentLoginRequest(
    string Username,
    string Password);

public sealed record AdministratorLoginRequest(
    string Email,
    string Password);
