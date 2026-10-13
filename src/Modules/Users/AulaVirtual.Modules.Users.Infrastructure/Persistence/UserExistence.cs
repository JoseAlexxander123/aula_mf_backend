using AulaVirtual.Modules.Users.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Users.Infrastructure.Persistence;

public sealed class UserExistence(UsersDbContext dbContext) : IUserExistence
{
    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken);
}
