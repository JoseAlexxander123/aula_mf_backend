using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Authorization.Domain;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Authorization.Infrastructure.Persistence;

public sealed class AuthorizationRepository(AuthorizationDbContext db) : IAuthorizationRepository
{
    public Task<Role?> GetRoleByCodeAsync(string code, CancellationToken token) =>
        db.Roles.SingleOrDefaultAsync(x => x.Code == code, token);
    public Task<Permission?> GetPermissionByCodeAsync(string code, CancellationToken token) =>
        db.Permissions.SingleOrDefaultAsync(x => x.Code == code, token);
    public Task<CatalogVersion?> GetCatalogVersionAsync(string code, CancellationToken token) =>
        db.CatalogVersions.SingleOrDefaultAsync(x => x.Code == code, token);
    public Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken token) =>
        db.UserRoles.AnyAsync(x => x.UserId == userId && x.RoleId == roleId, token);
    public async Task<bool> TryAssignFirstAdministratorAsync(Guid userId, Guid administratorRoleId,
        CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, token);
        // Every bootstrap locks the same persistent role row before checking or inserting UserRole.
        // PostgreSQL holds this row lock until commit, including across processes and app instances.
        var lockedRole = await db.Roles
            .FromSqlInterpolated($"SELECT * FROM \"authorization\".\"roles\" WHERE id = {administratorRoleId} FOR UPDATE")
            .AsNoTracking().ToListAsync(token);
        if (lockedRole.Count != 1)
            throw new InvalidOperationException("Administrator role no longer exists.");
        if (await db.UserRoles.AnyAsync(x => x.RoleId == administratorRoleId, token)) return false;

        db.UserRoles.Add(UserRole.Create(userId, administratorRoleId));
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return true;
    }
    public Task<bool> RoleHasPermissionAsync(Guid roleId, Guid permissionId, CancellationToken token) =>
        db.RolePermissions.AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId, token);
    public async Task<IReadOnlyList<string>> GetRoleCodesAsync(Guid userId, CancellationToken token) =>
        await db.UserRoles.AsNoTracking().Where(ur => ur.UserId == userId)
            .Join(db.Roles, ur => ur.RoleId, role => role.Id, (_, role) => role.Code)
            .Distinct().OrderBy(code => code).ToListAsync(token);
    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid userId, CancellationToken token) =>
        await (from ur in db.UserRoles.AsNoTracking()
               join rp in db.RolePermissions.AsNoTracking() on ur.RoleId equals rp.RoleId
               join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
               where ur.UserId == userId
               select p.Code).Distinct().OrderBy(code => code).ToListAsync(token);
    public Task<bool> HasPermissionAsync(Guid userId, string code, CancellationToken token) =>
        (from ur in db.UserRoles.AsNoTracking()
         join rp in db.RolePermissions.AsNoTracking() on ur.RoleId equals rp.RoleId
         join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
         where ur.UserId == userId && p.Code == code
         select p.Id).AnyAsync(token);
    public void Add(Role role) => db.Roles.Add(role);
    public void Add(Permission permission) => db.Permissions.Add(permission);
    public void Add(UserRole userRole) => db.UserRoles.Add(userRole);
    public void Add(RolePermission rolePermission) => db.RolePermissions.Add(rolePermission);
    public void Add(CatalogVersion version) => db.CatalogVersions.Add(version);
    public async Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken token)
    {
        var entity = await db.UserRoles.FindAsync([userId, roleId], token);
        if (entity is not null) db.UserRoles.Remove(entity);
    }
    public async Task RemoveRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken token)
    {
        var entity = await db.RolePermissions.FindAsync([roleId, permissionId], token);
        if (entity is not null) db.RolePermissions.Remove(entity);
    }
    public Task SaveChangesAsync(CancellationToken token) => db.SaveChangesAsync(token);
}

public sealed class PermissionChecker(IAuthorizationRepository repository) : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken token)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(permissionCode)) return Task.FromResult(false);
        return repository.HasPermissionAsync(userId, permissionCode, token);
    }
}
