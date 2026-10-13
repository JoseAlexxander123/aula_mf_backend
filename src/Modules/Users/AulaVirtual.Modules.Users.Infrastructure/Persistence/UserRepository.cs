using AulaVirtual.Modules.Users.Application.Abstractions;
using AulaVirtual.Modules.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Users.Infrastructure.Persistence;

public sealed class UserRepository(UsersDbContext dbContext) : IUserRepository
{
    public Task<bool> DocumentNumberExistsAsync(
        string documentNumber,
        CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(
            user => user.DocumentNumber == documentNumber,
            cancellationToken);
    }

    public void Add(User user)
    {
        dbContext.Users.Add(user);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
