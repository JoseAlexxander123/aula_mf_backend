using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AulaVirtual.Modules.Auth.Infrastructure.Persistence;

internal sealed class AuthDbContextFactory
    : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection must be provided externally.");
        }

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    "auth"))
            .ReplaceService<
                IHistoryRepository,
                SnakeCaseNpgsqlHistoryRepository>()
            .Options;

        return new AuthDbContext(options);
    }
}
