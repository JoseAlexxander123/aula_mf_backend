namespace AulaVirtual.Modules.Auth.Application.Accounts.CreateAccount;

public enum CreateAccountStatus
{
    Created,
    Invalid,
    DuplicateLogin,
    DuplicateDocument,
    UserCreationFailed
}

public sealed record CreateAccountResult(
    CreateAccountStatus Status,
    Guid? UserId = null,
    Guid? AuthAccountId = null,
    string? LoginIdentifier = null,
    string? InitialPassword = null,
    IDictionary<string, string[]>? Errors = null);
