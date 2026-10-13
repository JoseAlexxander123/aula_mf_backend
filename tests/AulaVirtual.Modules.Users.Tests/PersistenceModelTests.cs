using AulaVirtual.Modules.Users.Domain.Users;
using AulaVirtual.Modules.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Users.Tests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void UsersModel_UsesConfirmedSchemaTableAndFilteredUniqueIndex()
    {
        using var context = new UsersDbContext(
            new DbContextOptionsBuilder<UsersDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=model_only;Username=model")
                .Options);

        var entity = context.Model.FindEntityType(typeof(User))!;

        Assert.Equal("users", entity.GetSchema());
        Assert.Equal("users", entity.GetTableName());

        var documentIndex = Assert.Single(
            entity.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Single().Name ==
                    nameof(User.DocumentNumber));

        Assert.Equal(
            "document_number IS NOT NULL",
            documentIndex.GetFilter());
    }
}
