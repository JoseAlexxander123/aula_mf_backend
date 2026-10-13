namespace AulaVirtual.Modules.Auth.Api.Contracts;

public sealed record CurrentAccountResponse(
    Guid AuthAccountId,
    Guid UserId,
    IReadOnlyList<string> LoginTypes,
    string Status);
