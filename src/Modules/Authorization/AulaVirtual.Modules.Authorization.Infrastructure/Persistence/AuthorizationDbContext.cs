using AulaVirtual.Modules.Authorization.Domain;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Authorization.Infrastructure.Persistence;

public sealed class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<CatalogVersion> CatalogVersions => Set<CatalogVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("authorization");
        modelBuilder.Entity<Role>(builder =>
        {
            builder.ToTable("roles", "authorization");
            builder.HasKey(x => x.Id).HasName("pk_roles");
            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_roles_code");
        });
        modelBuilder.Entity<Permission>(builder =>
        {
            builder.ToTable("permissions", "authorization");
            builder.HasKey(x => x.Id).HasName("pk_permissions");
            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_permissions_code");
        });
        modelBuilder.Entity<UserRole>(builder =>
        {
            builder.ToTable("user_roles", "authorization");
            builder.HasKey(x => new { x.UserId, x.RoleId }).HasName("pk_user_roles");
            builder.Property(x => x.UserId).HasColumnName("user_id");
            builder.Property(x => x.RoleId).HasColumnName("role_id");
            builder.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_user_roles_role_id");
            builder.HasIndex(x => x.RoleId).HasDatabaseName("ix_user_roles_role_id");
        });
        modelBuilder.Entity<RolePermission>(builder =>
        {
            builder.ToTable("role_permissions", "authorization");
            builder.HasKey(x => new { x.RoleId, x.PermissionId }).HasName("pk_role_permissions");
            builder.Property(x => x.RoleId).HasColumnName("role_id");
            builder.Property(x => x.PermissionId).HasColumnName("permission_id");
            builder.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_role_permissions_role_id");
            builder.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_role_permissions_permission_id");
            builder.HasIndex(x => x.PermissionId).HasDatabaseName("ix_role_permissions_permission_id");
        });
        modelBuilder.Entity<CatalogVersion>(builder =>
        {
            builder.ToTable("catalog_versions", "authorization");
            builder.HasKey(x => x.Code).HasName("pk_catalog_versions");
            builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(100);
            builder.Property(x => x.Version).HasColumnName("version");
        });
    }
}
