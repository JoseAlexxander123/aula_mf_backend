using AulaVirtual.Modules.Auth.Infrastructure.Security;

namespace AulaVirtual.Modules.Auth.Tests;

public sealed class SecurityServiceTests
{
    [Fact]
    public void PasswordGenerator_ProducesRequiredTwelveCharacterPolicy()
    {
        var generator = new CryptographicPasswordGenerator();

        for (var sample = 0; sample < 50; sample++)
        {
            var password = generator.Generate();

            Assert.Equal(12, password.Length);
            Assert.Contains(password, char.IsUpper);
            Assert.Contains(password, char.IsLower);
            Assert.Contains(password, char.IsDigit);
            Assert.Contains(
                password,
                character => "!@#$%&*+-=?".Contains(character));
        }
    }

    [Fact]
    public void Argon2idHasher_UsesConfirmedParametersAndUniqueSalt()
    {
        var settings = new PasswordHashingSettings
        {
            MemorySizeKiB = 65536,
            Iterations = 2,
            Parallelism = 1,
            SaltSizeBytes = 16,
            HashSizeBytes = 32
        };
        var hasher = new Argon2idPasswordHasher(settings);

        var first = hasher.Hash("Correct-Horse-7!");
        var second = hasher.Hash("Correct-Horse-7!");

        Assert.StartsWith("$argon2id$v=19$m=65536,t=2,p=1$", first);
        Assert.NotEqual(first, second);
        Assert.True(hasher.Verify("Correct-Horse-7!", first));
        Assert.False(hasher.Verify("wrong-password", first));
    }

    [Fact]
    public void RefreshTokenService_GeneratesOpaqueTokenAndStoresOnlyHash()
    {
        var service = new RefreshTokenService();

        var token = service.Generate();
        var hash = service.ComputeHash(token);

        Assert.DoesNotContain(token, hash);
        Assert.Equal(43, token.Length);
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, service.ComputeHash(token));
    }
}
