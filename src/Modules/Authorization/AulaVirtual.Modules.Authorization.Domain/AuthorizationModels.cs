namespace AulaVirtual.Modules.Authorization.Domain;

public sealed class Role
{
    private Role() { }
    private Role(Guid id, string code, string name) { Id = id; Code = code; Name = name; }
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public static Role Create(Guid id, string code, string name) =>
        new(RequireId(id), RequireCode(code), RequireName(name));

    public static Guid RequireId(Guid id) => id != Guid.Empty ? id : throw new ArgumentException("Identifier cannot be empty.");
    public static string RequireCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        code = code.Trim();
        if (code.Length > 100 || code.Any(c => !((c >= 'a' && c <= 'z') || char.IsAsciiDigit(c) || c is '.' or '_')))
            throw new ArgumentException("Code must be a stable lower-case identifier.", nameof(code));
        return code;
    }
    public static string RequireName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        name = name.Trim();
        if (name.Length > 150) throw new ArgumentOutOfRangeException(nameof(name));
        return name;
    }
}

public sealed class Permission
{
    private Permission() { }
    private Permission(Guid id, string code, string name) { Id = id; Code = code; Name = name; }
    public Guid Id { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public static Permission Create(Guid id, string code, string name) =>
        new(Role.RequireId(id), Role.RequireCode(code), Role.RequireName(name));
}

public sealed class UserRole
{
    private UserRole() { }
    private UserRole(Guid userId, Guid roleId) { UserId = userId; RoleId = roleId; }
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public static UserRole Create(Guid userId, Guid roleId) =>
        new(Role.RequireId(userId), Role.RequireId(roleId));
}

public sealed class RolePermission
{
    private RolePermission() { }
    private RolePermission(Guid roleId, Guid permissionId) { RoleId = roleId; PermissionId = permissionId; }
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public static RolePermission Create(Guid roleId, Guid permissionId) =>
        new(Role.RequireId(roleId), Role.RequireId(permissionId));
}

public sealed class CatalogVersion
{
    private CatalogVersion() { }
    private CatalogVersion(string code, int version) { Code = code; Version = version; }
    public string Code { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public static CatalogVersion Create(string code, int version) =>
        new(Role.RequireCode(code), version > 0 ? version : throw new ArgumentOutOfRangeException(nameof(version)));
    public void AdvanceTo(int version)
    {
        if (version < Version) throw new InvalidOperationException("Catalog version cannot move backward.");
        Version = version;
    }
}
