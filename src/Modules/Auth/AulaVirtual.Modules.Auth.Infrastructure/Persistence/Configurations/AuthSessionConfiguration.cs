using AulaVirtual.Modules.Auth.Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Auth.Infrastructure.Persistence.Configurations;

internal sealed class AuthSessionConfiguration
    : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> builder)
    {
        builder.ToTable("auth_sessions", "auth");

        builder.HasKey(session => session.Id)
            .HasName("pk_auth_sessions");

        builder.Property(session => session.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(session => session.AuthAccountId)
            .HasColumnName("auth_account_id")
            .IsRequired();

        builder.Property(session => session.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(session => session.LastUsedAt)
            .HasColumnName("last_used_at")
            .IsRequired();

        builder.Property(session => session.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(session => session.RevokedAt)
            .HasColumnName("revoked_at");

        builder.HasIndex(session => session.AuthAccountId)
            .HasDatabaseName("ix_auth_sessions_auth_account_id");

        builder.HasMany(session => session.RefreshTokens)
            .WithOne()
            .HasForeignKey(token => token.SessionId)
            .HasConstraintName(
                "fk_refresh_tokens_auth_sessions_session_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
