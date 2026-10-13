using AulaVirtual.Modules.Auth.Application.Authentication;
using AulaVirtual.Modules.Auth.Application.Authentication.Login;
using AulaVirtual.Modules.Auth.Domain.Accounts;

namespace AulaVirtual.Modules.Auth.Tests;

public sealed class LoginHandlerTests
{
    [Theory]
    [InlineData(LoginType.Username, "  g77340061  ", "G77340061")]
    [InlineData(LoginType.Email, " Admin@Empresa.com ", "ADMIN@EMPRESA.COM")]
    public async Task Login_NormalizesIdentifierAndCreatesSession(
        LoginType loginType,
        string suppliedIdentifier,
        string storedNormalizedIdentifier)
    {
        var fixture = CreateFixture(loginType, storedNormalizedIdentifier);

        var result = await fixture.Handler.ExecuteAsync(
            new LoginCommand(
                loginType,
                suppliedIdentifier,
                "valid-password"),
            CancellationToken.None);

        Assert.Equal(AuthenticationStatus.Succeeded, result.Status);
        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token-2", result.RefreshToken);
        Assert.Single(fixture.Account.Sessions);
        Assert.Equal(0, fixture.Account.FailedLoginAttempts);
        Assert.Equal(1, fixture.Repository.SaveChangesCalls);
    }

    [Fact]
    public async Task Login_InactiveAccount_ReturnsGenericInvalidCredentials()
    {
        var fixture = CreateFixture(
            LoginType.Username,
            "G77340061");
        fixture.Account.Deactivate(fixture.TimeProvider.GetUtcNow());

        var result = await fixture.Handler.ExecuteAsync(
            new LoginCommand(
                LoginType.Username,
                "G77340061",
                "valid-password"),
            CancellationToken.None);

        Assert.Equal(
            AuthenticationStatus.InvalidCredentials,
            result.Status);
        Assert.Null(result.AccessToken);
    }

    [Fact]
    public async Task Login_InvalidPasswordLocksAfterFiveFailures()
    {
        var fixture = CreateFixture(
            LoginType.Username,
            "G77340061");

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var invalidResult = await fixture.Handler.ExecuteAsync(
                new LoginCommand(
                    LoginType.Username,
                    "G77340061",
                    "wrong-password"),
                CancellationToken.None);

            Assert.Equal(
                AuthenticationStatus.InvalidCredentials,
                invalidResult.Status);
        }

        var lockedResult = await fixture.Handler.ExecuteAsync(
            new LoginCommand(
                LoginType.Username,
                "G77340061",
                "valid-password"),
            CancellationToken.None);

        Assert.Equal(AuthenticationStatus.Locked, lockedResult.Status);

        fixture.TimeProvider.Advance(TimeSpan.FromMinutes(10));

        var recoveredResult = await fixture.Handler.ExecuteAsync(
            new LoginCommand(
                LoginType.Username,
                "G77340061",
                "valid-password"),
            CancellationToken.None);

        Assert.Equal(
            AuthenticationStatus.Succeeded,
            recoveredResult.Status);
        Assert.Equal(0, fixture.Account.FailedLoginAttempts);
    }

    [Fact]
    public async Task Login_WithEitherIdentifier_UsesTheSameAccountState()
    {
        var fixture = CreateFixture(
            LoginType.Username,
            "G77340061");
        fixture.Account.AddIdentifier(
            Guid.NewGuid(),
            LoginType.Email,
            "student@example.com",
            fixture.TimeProvider.GetUtcNow());

        for (var attempt = 0; attempt < 5; attempt++)
        {
            await fixture.Handler.ExecuteAsync(
                new LoginCommand(
                    LoginType.Username,
                    "G77340061",
                    "wrong-password"),
                CancellationToken.None);
        }

        var result = await fixture.Handler.ExecuteAsync(
            new LoginCommand(
                LoginType.Email,
                " Student@Example.com ",
                "valid-password"),
            CancellationToken.None);

        Assert.Equal(AuthenticationStatus.Locked, result.Status);
        Assert.Equal(5, fixture.Account.FailedLoginAttempts);
        Assert.Empty(fixture.Account.Sessions);
    }

    [Fact]
    public async Task Login_WithUsernameAndEmail_StartsSessionsOnTheSameAccount()
    {
        var fixture = CreateFixture(
            LoginType.Username,
            "G77340061");
        fixture.Account.AddIdentifier(
            Guid.NewGuid(),
            LoginType.Email,
            "student@example.com",
            fixture.TimeProvider.GetUtcNow());

        var usernameResult = await fixture.Handler.ExecuteAsync(
            new LoginCommand(
                LoginType.Username,
                "g77340061",
                "valid-password"),
            CancellationToken.None);
        var emailResult = await fixture.Handler.ExecuteAsync(
            new LoginCommand(
                LoginType.Email,
                "Student@Example.com",
                "valid-password"),
            CancellationToken.None);

        Assert.Equal(AuthenticationStatus.Succeeded, usernameResult.Status);
        Assert.Equal(AuthenticationStatus.Succeeded, emailResult.Status);
        Assert.Equal(2, fixture.Account.Sessions.Count);
        Assert.Equal(0, fixture.Account.FailedLoginAttempts);
    }

    private static LoginFixture CreateFixture(
        LoginType loginType,
        string identifier)
    {
        var timeProvider = new MutableTimeProvider(
            new DateTimeOffset(
                2026,
                9,
                25,
                10,
                0,
                0,
                TimeSpan.Zero));
        var repository = new FakeAuthRepository();
        var account = AuthAccount.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "encoded",
            timeProvider.GetUtcNow());
        account.AddIdentifier(
            Guid.NewGuid(),
            loginType,
            identifier,
            timeProvider.GetUtcNow());
        repository.Accounts.Add(account);

        var handler = new LoginHandler(
            repository,
            new StubPasswordHasher("valid-password"),
            new SequentialRefreshTokenService(),
            new StubAccessTokenService(),
            new LoginCommandValidator(),
            timeProvider,
            new AuthenticationSettings());

        return new LoginFixture(
            handler,
            repository,
            account,
            timeProvider);
    }

    private sealed record LoginFixture(
        LoginHandler Handler,
        FakeAuthRepository Repository,
        AuthAccount Account,
        MutableTimeProvider TimeProvider);
}
