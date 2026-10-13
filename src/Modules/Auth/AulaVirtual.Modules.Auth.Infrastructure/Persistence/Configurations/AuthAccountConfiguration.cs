using AulaVirtual.Modules.Auth.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthAccountConfiguration
    : IEntityTypeConfiguration<AuthAccount>
{
    public void Configure(EntityTypeBuilder<AuthAccount> builder)
    {
        builder.ToTable(
            "auth_accounts",
            "auth",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "auth_accounts_status_check",
                    "status IN ('active', 'inactive')");
            });

        builder.HasKey(account => account.Id)
            .HasName("pk_auth_accounts");

        builder.Property(account => account.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(account => account.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(account => account.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(account => account.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion(
                value => value == AccountStatus.Active
                    ? "active"
                    : "inactive",
                value => value == "active"
                    ? AccountStatus.Active
                    : AccountStatus.Inactive)
            .IsRequired();

        builder.Property(account => account.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(account => account.LockedUntil)
            .HasColumnName("locked_until");

        builder.Property(account => account.PasswordChangedAt)
            .HasColumnName("password_changed_at");

        builder.Property(account => account.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(account => account.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(account => account.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(account => account.UserId)
            .HasDatabaseName("ix_auth_accounts_user_id")
            .IsUnique();

        builder.HasMany(account => account.Identifiers)
            .WithOne()
            .HasForeignKey(identifier => identifier.AuthAccountId)
            .HasConstraintName(
                "fk_auth_identifiers_auth_accounts_auth_account_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(account => account.Sessions)
            .WithOne()
            .HasForeignKey(session => session.AuthAccountId)
            .HasConstraintName(
                "fk_auth_sessions_auth_accounts_auth_account_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
