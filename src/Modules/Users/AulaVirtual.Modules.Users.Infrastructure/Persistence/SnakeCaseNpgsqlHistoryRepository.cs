using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal;

namespace AulaVirtual.Modules.Users.Infrastructure.Persistence;

#pragma warning disable EF1001
public sealed class SnakeCaseNpgsqlHistoryRepository(
    HistoryRepositoryDependencies dependencies)
    : NpgsqlHistoryRepository(dependencies)
{
    protected override void ConfigureTable(EntityTypeBuilder<HistoryRow> history)
    {
        base.ConfigureTable(history);

        history.Property(row => row.MigrationId)
            .HasColumnName("migration_id");
        history.Property(row => row.ProductVersion)
            .HasColumnName("product_version");
        history.HasKey(row => row.MigrationId)
            .HasName("pk___ef_migrations_history");
    }
}
#pragma warning restore EF1001
