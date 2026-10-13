using AulaVirtual.Modules.Authorization.Domain;

namespace AulaVirtual.Modules.Authorization.Application;

public interface IAuthorizationRepository
{
    Task<Role?> GetRoleByCodeAsync(string code, CancellationToken cancellationToken);
    Task<Permission?> GetPermissionByCodeAsync(string code, CancellationToken cancellationToken);
    Task<CatalogVersion?> GetCatalogVersionAsync(string code, CancellationToken cancellationToken);
    Task<bool> UserHasRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);
    Task<bool> TryAssignFirstAdministratorAsync(Guid userId, Guid administratorRoleId,
        CancellationToken cancellationToken);
    Task<bool> RoleHasPermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetRoleCodesAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken);
    void Add(Role role);
    void Add(Permission permission);
    void Add(UserRole userRole);
    void Add(RolePermission rolePermission);
    void Add(CatalogVersion version);
    Task RemoveUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken);
    Task RemoveRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken);
}
