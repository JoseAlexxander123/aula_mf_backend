namespace AulaVirtual.Modules.Auth.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string encodedHash);
}
