using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence;

internal sealed class CoursesDbContextFactory
    : IDesignTimeDbContextFactory<CoursesDbContext>
{
    public CoursesDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings__DefaultConnection must be provided externally.");
        }

        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    "courses"))
            .ReplaceService<IHistoryRepository, SnakeCaseNpgsqlHistoryRepository>()
            .Options;

        return new CoursesDbContext(options);
    }
}
