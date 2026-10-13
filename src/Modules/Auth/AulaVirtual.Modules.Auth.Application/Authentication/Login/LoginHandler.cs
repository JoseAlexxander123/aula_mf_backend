using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Domain.Accounts;
using FluentValidation;

namespace AulaVirtual.Modules.Auth.Application.Authentication.Login;

public sealed class LoginHandler(
    IAuthRepository repository,
    IPasswordHasher passwordHasher,
    IRefreshTokenService refreshTokenService,
    IAccessTokenService accessTokenService,
    IValidator<LoginCommand> validator,
    TimeProvider timeProvider,
    AuthenticationSettings settings)
{
    public async Task<AuthenticationResult> ExecuteAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            return new AuthenticationResult(
                AuthenticationStatus.InvalidRequest);
        }

        var normalizedIdentifier =
            command.Identifier.Trim().ToUpperInvariant();

        var account = await repository.FindAccountByIdentifierAsync(
            command.LoginType,
            normalizedIdentifier,
            cancellationToken);

        if (account is null || account.Status != AccountStatus.Active)
        {
            return new AuthenticationResult(
                AuthenticationStatus.InvalidCredentials);
        }

        var now = timeProvider.GetUtcNow();

        if (account.IsLocked(now))
        {
            return new AuthenticationResult(AuthenticationStatus.Locked);
        }

        if (!passwordHasher.Verify(command.Password, account.PasswordHash))
        {
            account.RegisterFailedLogin(
                now,
                settings.MaximumFailedLoginAttempts,
                TimeSpan.FromMinutes(settings.LockoutMinutes));

            await repository.SaveChangesAsync(cancellationToken);

            return new AuthenticationResult(
                AuthenticationStatus.InvalidCredentials);
        }

        account.RegisterSuccessfulLogin(now);

        var refreshTokenValue = refreshTokenService.Generate();
        var refreshTokenHash =
            refreshTokenService.ComputeHash(refreshTokenValue);
        var refreshTokenExpiration =
            now.AddDays(settings.RefreshTokenDays);

        var session = account.StartSession(
            Guid.NewGuid(),
            now,
            refreshTokenExpiration);

        session.AddRefreshToken(
            Guid.NewGuid(),
            refreshTokenHash,
            now,
            refreshTokenExpiration);

        var accessToken = accessTokenService.Generate(account);

        await repository.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            AuthenticationStatus.Succeeded,
            accessToken.Value,
            accessToken.TokenType,
            accessToken.ExpiresIn,
            refreshTokenValue);
    }
}
