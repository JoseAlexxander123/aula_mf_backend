using AulaVirtual.Modules.Auth.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthIdentifierConfiguration
    : IEntityTypeConfiguration<AuthIdentifier>
{
    public void Configure(EntityTypeBuilder<AuthIdentifier> builder)
    {
        builder.ToTable(
            "auth_identifiers",
            "auth",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "auth_identifiers_login_type_check",
                "login_type IN ('username', 'email')"));

        builder.HasKey(identifier => identifier.Id)
            .HasName("pk_auth_identifiers");

        builder.Property(identifier => identifier.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(identifier => identifier.AuthAccountId)
            .HasColumnName("auth_account_id")
            .IsRequired();

        builder.Property(identifier => identifier.LoginType)
            .HasColumnName("login_type")
            .HasMaxLength(32)
            .HasConversion(
                value => value == LoginType.Username
                    ? "username"
                    : "email",
                value => value == "username"
                    ? LoginType.Username
                    : LoginType.Email)
            .IsRequired();

        builder.Property(identifier => identifier.LoginIdentifier)
            .HasColumnName("login_identifier")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(identifier => identifier.NormalizedLoginIdentifier)
            .HasColumnName("normalized_login_identifier")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(identifier => identifier.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(identifier => identifier.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(identifier => new
            {
                identifier.AuthAccountId,
                identifier.LoginType
            })
            .HasDatabaseName(
                "ix_auth_identifiers_auth_account_id_login_type")
            .IsUnique();

        builder.HasIndex(identifier => new
            {
                identifier.LoginType,
                identifier.NormalizedLoginIdentifier
            })
            .HasDatabaseName(
                "ix_auth_identifiers_login_type_normalized_login_identifier")
            .IsUnique();
    }
}
