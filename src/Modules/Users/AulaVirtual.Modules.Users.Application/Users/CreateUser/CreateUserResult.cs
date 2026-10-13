namespace AulaVirtual.Modules.Users.Application.Users.CreateUser;

public enum CreateUserStatus
{
    Created,
    DuplicateDocument,
    Invalid
}

public sealed record CreateUserResult(
    CreateUserStatus Status,
    Guid? UserId = null,
    IDictionary<string, string[]>? Errors = null);
