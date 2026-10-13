using AulaVirtual.Modules.Users.Domain.Users;

namespace AulaVirtual.Modules.Users.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> DocumentNumberExistsAsync(
        string documentNumber,
        CancellationToken cancellationToken);

    void Add(User user);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
