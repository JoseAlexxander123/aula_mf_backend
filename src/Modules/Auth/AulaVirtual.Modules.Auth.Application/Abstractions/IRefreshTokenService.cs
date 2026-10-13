namespace AulaVirtual.Modules.Auth.Application.Abstractions;

public interface IRefreshTokenService
{
    string Generate();

    string ComputeHash(string token);
}
