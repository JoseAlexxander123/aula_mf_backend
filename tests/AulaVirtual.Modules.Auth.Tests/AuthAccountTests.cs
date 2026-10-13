using AulaVirtual.Modules.Auth.Domain.Accounts;

namespace AulaVirtual.Modules.Auth.Tests;

public sealed class AuthAccountTests
{
    [Fact]
    public void Create_NormalizesIdentifierUsingInvariantUppercase()
    {
        var account = CreateAccount("  admin@Empresa.com  ");
        var identifier = Assert.Single(account.Identifiers);

        Assert.Equal("admin@Empresa.com", identifier.LoginIdentifier);
        Assert.Equal(
            "ADMIN@EMPRESA.COM",
            identifier.NormalizedLoginIdentifier);
        Assert.Equal(AccountStatus.Active, account.Status);
    }

    [Fact]
    public void AddIdentifier_AllowsDifferentTypesOnTheSameAccount()
    {
        var account = CreateAccount("student");

        account.AddIdentifier(
            Guid.NewGuid(),
            LoginType.Username,
            " Student01 ",
            DateTimeOffset.UtcNow);

        Assert.Equal(2, account.Identifiers.Count);
        Assert.Contains(
            account.Identifiers,
            identifier =>
                identifier.LoginType == LoginType.Username &&
                identifier.NormalizedLoginIdentifier ==
                    "STUDENT01");
    }

    [Fact]
    public void AddIdentifier_RejectsDuplicateTypeWithinTheAccount()
    {
        var account = CreateAccount("student");

        Assert.Throws<InvalidOperationException>(() =>
            account.AddIdentifier(
                Guid.NewGuid(),
                LoginType.Email,
                "other@example.com",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void FiveFailures_LockAccountForTenMinutes()
    {
        var now = new DateTimeOffset(
            2026,
            9,
            25,
            10,
            0,
            0,
            TimeSpan.Zero);
        var account = CreateAccount("student");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            account.RegisterFailedLogin(
                now,
                5,
                TimeSpan.FromMinutes(10));
        }

        Assert.Equal(5, account.FailedLoginAttempts);
        Assert.True(account.IsLocked(now.AddMinutes(9)));
        Assert.False(account.IsLocked(now.AddMinutes(10)));
    }

    [Fact]
    public void SuccessfulLogin_ResetsFailuresAndLockout()
    {
        var now = DateTimeOffset.UtcNow;
        var account = CreateAccount("student");

        account.RegisterFailedLogin(now, 1, TimeSpan.FromMinutes(10));
        account.RegisterSuccessfulLogin(now.AddMinutes(1));

        Assert.Equal(0, account.FailedLoginAttempts);
        Assert.Null(account.LockedUntil);
        Assert.Equal(now.AddMinutes(1), account.LastLoginAt);
    }

    private static AuthAccount CreateAccount(string identifier)
    {
        var account = AuthAccount.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "password-hash",
            DateTimeOffset.UtcNow);
        account.AddIdentifier(
            Guid.NewGuid(),
            LoginType.Email,
            identifier,
            DateTimeOffset.UtcNow);

        return account;
    }
}
