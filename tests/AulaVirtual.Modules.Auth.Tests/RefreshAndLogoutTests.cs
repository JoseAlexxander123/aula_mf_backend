using AulaVirtual.Modules.Auth.Application.Authentication;
using AulaVirtual.Modules.Auth.Application.Authentication.Logout;
using AulaVirtual.Modules.Auth.Application.Authentication.Refresh;
using AulaVirtual.Modules.Auth.Domain.Accounts;

namespace AulaVirtual.Modules.Auth.Tests;

public sealed class RefreshAndLogoutTests
{
    [Fact]
    public async Task Refresh_RotatesTokenWithinSameSession()
    {
        var fixture = CreateFixture();

        var result = await fixture.RefreshHandler.ExecuteAsync(
            "refresh-token-1",
            CancellationToken.None);

        Assert.Equal(AuthenticationStatus.Succeeded, result.Status);
        Assert.Equal("refresh-token-2", result.RefreshToken);
        Assert.NotNull(fixture.OriginalToken.UsedAt);
        Assert.NotNull(fixture.OriginalToken.ReplacedByTokenId);
        Assert.Equal(2, fixture.Session.RefreshTokens.Count);
        Assert.All(
            fixture.Repository.Accounts,
            account => Assert.Single(account.Sessions));
    }

    [Fact]
    public async Task ReusingRotatedToken_RevokesOnlyItsSession()
    {
        var fixture = CreateFixture();
        var otherSession = fixture.Account.StartSession(
            Guid.NewGuid(),
            fixture.TimeProvider.GetUtcNow(),
            fixture.TimeProvider.GetUtcNow().AddDays(7));
        otherSession.AddRefreshToken(
            Guid.NewGuid(),
            "hash:other-token",
            fixture.TimeProvider.GetUtcNow(),
            fixture.TimeProvider.GetUtcNow().AddDays(7));

        await fixture.RefreshHandler.ExecuteAsync(
            "refresh-token-1",
            CancellationToken.None);

        var reuseResult = await fixture.RefreshHandler.ExecuteAsync(
            "refresh-token-1",
            CancellationToken.None);

        Assert.Equal(
            AuthenticationStatus.ReusedRefreshToken,
            reuseResult.Status);
        Assert.False(
            fixture.Session.IsActive(fixture.TimeProvider.GetUtcNow()));
        Assert.True(
            otherSession.IsActive(fixture.TimeProvider.GetUtcNow()));
    }

    [Fact]
    public async Task LogoutCurrent_IsIdempotent()
    {
        var fixture = CreateFixture();
        var handler = new LogoutCurrentSessionHandler(
            fixture.Repository,
            fixture.RefreshTokenService,
            fixture.TimeProvider);

        await handler.ExecuteAsync(
            "refresh-token-1",
            CancellationToken.None);
        await handler.ExecuteAsync(
            "refresh-token-1",
            CancellationToken.None);
        await handler.ExecuteAsync(null, CancellationToken.None);

        Assert.NotNull(fixture.Session.RevokedAt);
    }

    [Fact]
    public async Task LogoutAll_RevokesEverySession()
    {
        var fixture = CreateFixture();
        fixture.Account.StartSession(
            Guid.NewGuid(),
            fixture.TimeProvider.GetUtcNow(),
            fixture.TimeProvider.GetUtcNow().AddDays(7));

        var handler = new LogoutAllSessionsHandler(
            fixture.Repository,
            fixture.TimeProvider);

        await handler.ExecuteAsync(
            fixture.Account.Id,
            CancellationToken.None);

        Assert.All(
            fixture.Account.Sessions,
            session => Assert.NotNull(session.RevokedAt));
    }

    private static RefreshFixture CreateFixture()
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
            LoginType.Username,
            "G77340061",
            timeProvider.GetUtcNow());
        repository.Accounts.Add(account);

        var session = account.StartSession(
            Guid.NewGuid(),
            timeProvider.GetUtcNow(),
            timeProvider.GetUtcNow().AddDays(7));
        var originalToken = session.AddRefreshToken(
            Guid.NewGuid(),
            "hash:refresh-token-1",
            timeProvider.GetUtcNow(),
            timeProvider.GetUtcNow().AddDays(7));
        var refreshTokenService = new SequentialRefreshTokenService();
        var handler = new RefreshSessionHandler(
            repository,
            refreshTokenService,
            new StubAccessTokenService(),
            timeProvider,
            new AuthenticationSettings());

        return new RefreshFixture(
            handler,
            repository,
            refreshTokenService,
            account,
            session,
            originalToken,
            timeProvider);
    }

    private sealed record RefreshFixture(
        RefreshSessionHandler RefreshHandler,
        FakeAuthRepository Repository,
        SequentialRefreshTokenService RefreshTokenService,
        AuthAccount Account,
        AulaVirtual.Modules.Auth.Domain.Sessions.AuthSession Session,
        AulaVirtual.Modules.Auth.Domain.Sessions.RefreshToken OriginalToken,
        MutableTimeProvider TimeProvider);
}
