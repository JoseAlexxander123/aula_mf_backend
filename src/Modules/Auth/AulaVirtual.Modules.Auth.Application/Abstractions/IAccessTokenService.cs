using AulaVirtual.Modules.Auth.Domain.Accounts;

namespace AulaVirtual.Modules.Auth.Application.Abstractions;

public interface IAccessTokenService
{
    AccessToken Generate(AuthAccount account);
}

public sealed record AccessToken(
    string Value,
    string TokenType,
    int ExpiresIn);
