using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Domain.Accounts;

namespace AulaVirtual.Modules.Auth.Application.Authentication.GetCurrentAccount;

public sealed class GetCurrentAccountHandler(IAuthRepository repository)
{
    public async Task<CurrentAccountResult?> ExecuteAsync(
        Guid authAccountId,
        CancellationToken cancellationToken)
    {
        var account = await repository.FindAccountByIdAsync(
            authAccountId,
            cancellationToken);

        return account is null
            ? null
            : new CurrentAccountResult(
                account.Id,
                account.UserId,
                account.Identifiers
                    .Select(identifier => identifier.LoginType)
                    .OrderBy(loginType => loginType)
                    .ToArray(),
                account.Status);
    }
}

public sealed record CurrentAccountResult(
    Guid AuthAccountId,
    Guid UserId,
    IReadOnlyList<LoginType> LoginTypes,
    AccountStatus Status);
