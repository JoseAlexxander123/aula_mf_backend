using System.Security.Cryptography;
using System.Text;
using AulaVirtual.Modules.Auth.Application.Abstractions;
using Konscious.Security.Cryptography;

namespace AulaVirtual.Modules.Auth.Infrastructure.Security;

public sealed class Argon2idPasswordHasher(PasswordHashingSettings settings)
    : IPasswordHasher
{
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(settings.SaltSizeBytes);
        var hash = DeriveKey(
            password,
            salt,
            settings.MemorySizeKiB,
            settings.Iterations,
            settings.Parallelism,
            settings.HashSizeBytes);

        return "$argon2id$v=19$m=" + settings.MemorySizeKiB +
            ",t=" + settings.Iterations +
            ",p=" + settings.Parallelism +
            "$" + Convert.ToBase64String(salt) +
            "$" + Convert.ToBase64String(hash);
    }

    public bool Verify(string password, string encodedHash)
    {
        if (string.IsNullOrEmpty(password) ||
            !TryParse(encodedHash, out var parameters))
        {
            return false;
        }

        var actual = DeriveKey(
            password,
            parameters.Salt,
            parameters.MemorySizeKiB,
            parameters.Iterations,
            parameters.Parallelism,
            parameters.Hash.Length);

        return CryptographicOperations.FixedTimeEquals(
            actual,
            parameters.Hash);
    }

    private static byte[] DeriveKey(
        string password,
        byte[] salt,
        int memorySizeKiB,
        int iterations,
        int parallelism,
        int hashSizeBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySizeKiB,
            Iterations = iterations,
            DegreeOfParallelism = parallelism
        };

        return argon2.GetBytes(hashSizeBytes);
    }

    private static bool TryParse(
        string encodedHash,
        out HashParameters parameters)
    {
        parameters = default;

        try
        {
            var parts = encodedHash.Split('$', StringSplitOptions.None);

            if (parts.Length != 6 ||
                parts[1] != "argon2id" ||
                parts[2] != "v=19")
            {
                return false;
            }

            var values = parts[3]
                .Split(',')
                .Select(part => part.Split('=', 2))
                .ToDictionary(
                    pair => pair[0],
                    pair => int.Parse(
                        pair[1],
                        System.Globalization.CultureInfo.InvariantCulture));

            var salt = Convert.FromBase64String(parts[4]);
            var hash = Convert.FromBase64String(parts[5]);

            parameters = new HashParameters(
                values["m"],
                values["t"],
                values["p"],
                salt,
                hash);

            return parameters.MemorySizeKiB > 0 &&
                parameters.Iterations > 0 &&
                parameters.Parallelism > 0 &&
                parameters.Salt.Length > 0 &&
                parameters.Hash.Length > 0;
        }
        catch (Exception exception) when (
            exception is FormatException or
            KeyNotFoundException or
            IndexOutOfRangeException or
            OverflowException or
            ArgumentException)
        {
            return false;
        }
    }

    private readonly record struct HashParameters(
        int MemorySizeKiB,
        int Iterations,
        int Parallelism,
        byte[] Salt,
        byte[] Hash);
}
