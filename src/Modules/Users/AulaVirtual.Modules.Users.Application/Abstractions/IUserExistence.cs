namespace AulaVirtual.Modules.Users.Application.Abstractions;

// Cross-module contract: callers receive existence only, never Users persistence.
public interface IUserExistence
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);
}
