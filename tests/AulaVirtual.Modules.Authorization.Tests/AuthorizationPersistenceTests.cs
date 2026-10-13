using AulaVirtual.Modules.Authorization.Domain;
using AulaVirtual.Modules.Authorization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AulaVirtual.Modules.Authorization.Tests;

public sealed class AuthorizationPersistenceTests
{
    [Fact]
    public void ModelUsesOwnSchemaUniqueCodesAndCompositeAssignments()
    {
        using var db = new AuthorizationDbContext(new DbContextOptionsBuilder<AuthorizationDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model").Options);
        var model = db.GetService<IDesignTimeModel>().Model;
        var role = model.FindEntityType(typeof(Role))!;
        var permission = model.FindEntityType(typeof(Permission))!;
        var userRole = model.FindEntityType(typeof(UserRole))!;
        var rolePermission = model.FindEntityType(typeof(RolePermission))!;
        Assert.Equal("authorization", role.GetSchema());
        Assert.Equal("authorization", permission.GetSchema());
        Assert.Equal("authorization", userRole.GetSchema());
        Assert.Equal("authorization", rolePermission.GetSchema());
        Assert.Contains(role.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Role.Code));
        Assert.Contains(permission.GetIndexes(), index => index.IsUnique && index.Properties.Single().Name == nameof(Permission.Code));
        Assert.Equal(2, userRole.FindPrimaryKey()!.Properties.Count);
        Assert.Equal(2, rolePermission.FindPrimaryKey()!.Properties.Count);
        Assert.Single(userRole.GetForeignKeys());
        Assert.Equal(typeof(Role), userRole.GetForeignKeys().Single().PrincipalEntityType.ClrType);
        Assert.Equal(2, rolePermission.GetForeignKeys().Count());
        Assert.NotNull(model.FindEntityType(typeof(CatalogVersion)));
    }
}
