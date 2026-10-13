using AulaVirtual.Modules.Auth.Domain.Accounts;

namespace AulaVirtual.Modules.Auth.Application.Authentication.Login;

public sealed record LoginCommand(
    LoginType LoginType,
    string Identifier,
    string Password);
