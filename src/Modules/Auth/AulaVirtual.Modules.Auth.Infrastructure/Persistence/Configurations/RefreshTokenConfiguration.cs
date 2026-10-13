using AulaVirtual.Modules.Auth.Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Auth.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", "auth");

        builder.HasKey(token => token.Id)
            .HasName("pk_refresh_tokens");

        builder.Property(token => token.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(token => token.SessionId)
            .HasColumnName("session_id")
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(token => token.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(token => token.UsedAt)
            .HasColumnName("used_at");

        builder.Property(token => token.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(token => token.ReplacedByTokenId)
            .HasColumnName("replaced_by_token_id");

        builder.HasIndex(token => token.SessionId)
            .HasDatabaseName("ix_refresh_tokens_session_id");

        builder.HasIndex(token => token.TokenHash)
            .HasDatabaseName("ix_refresh_tokens_token_hash")
            .IsUnique();

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(token => token.ReplacedByTokenId)
            .HasConstraintName(
                "fk_refresh_tokens_refresh_tokens_replaced_by_token_id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
