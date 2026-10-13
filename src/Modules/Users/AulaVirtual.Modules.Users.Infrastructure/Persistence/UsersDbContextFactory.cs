using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AulaVirtual.Modules.Users.Infrastructure.Persistence;

internal sealed class UsersDbContextFactory
    : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection must be provided externally.");
        }

        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    "users"))
            .ReplaceService<
                IHistoryRepository,
                SnakeCaseNpgsqlHistoryRepository>()
            .Options;

        return new UsersDbContext(options);
    }
}
