using System.Security.Cryptography;
using AulaVirtual.Modules.Auth.Application.Abstractions;

namespace AulaVirtual.Modules.Auth.Infrastructure.Security;

public sealed class CryptographicPasswordGenerator : IPasswordGenerator
{
    private const int PasswordLength = 12;
    private const string Uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%&*+-=?";
    private const string AllCharacters =
        Uppercase + Lowercase + Digits + Symbols;

    public string Generate()
    {
        Span<char> password = stackalloc char[PasswordLength];

        password[0] = Pick(Uppercase);
        password[1] = Pick(Lowercase);
        password[2] = Pick(Digits);
        password[3] = Pick(Symbols);

        for (var index = 4; index < password.Length; index++)
        {
            password[index] = Pick(AllCharacters);
        }

        for (var index = password.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[swapIndex]) =
                (password[swapIndex], password[index]);
        }

        return new string(password);
    }

    private static char Pick(string characters)
    {
        return characters[RandomNumberGenerator.GetInt32(characters.Length)];
    }
}
