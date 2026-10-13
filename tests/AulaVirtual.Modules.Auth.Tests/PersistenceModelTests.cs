using AulaVirtual.Modules.Auth.Domain.Accounts;
using AulaVirtual.Modules.Auth.Domain.Sessions;
using AulaVirtual.Modules.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Auth.Tests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void AuthModel_UsesConfirmedTablesColumnsAndRelationships()
    {
        using var context = new AuthDbContext(
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseNpgsql(
                    "Host=localhost;Database=model_only;Username=model")
                .Options);

        var account = context.Model.FindEntityType(typeof(AuthAccount))!;
        var identifier = context.Model.FindEntityType(typeof(AuthIdentifier))!;
        var session = context.Model.FindEntityType(typeof(AuthSession))!;
        var token = context.Model.FindEntityType(typeof(RefreshToken))!;

        Assert.Equal("auth", account.GetSchema());
        Assert.Equal("auth_accounts", account.GetTableName());
        Assert.Equal("auth", identifier.GetSchema());
        Assert.Equal("auth_identifiers", identifier.GetTableName());
        Assert.Equal("auth", session.GetSchema());
        Assert.Equal("auth_sessions", session.GetTableName());
        Assert.Equal("auth", token.GetSchema());
        Assert.Equal("refresh_tokens", token.GetTableName());

        Assert.Null(account.FindNavigation(nameof(AuthAccount.UserId)));
        Assert.DoesNotContain(
            account.GetForeignKeys(),
            foreignKey => foreignKey.Properties.Any(
                property => property.Name == nameof(AuthAccount.UserId)));

        Assert.Contains(
            account.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Single().Name == nameof(AuthAccount.UserId));

        Assert.Contains(
            identifier.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual([
                        nameof(AuthIdentifier.AuthAccountId),
                        nameof(AuthIdentifier.LoginType)
                    ]));
        Assert.Contains(
            identifier.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual([
                        nameof(AuthIdentifier.LoginType),
                        nameof(AuthIdentifier.NormalizedLoginIdentifier)
                    ]));
        var identifierForeignKey = Assert.Single(
            identifier.GetForeignKeys());
        Assert.Equal(account, identifierForeignKey.PrincipalEntityType);
        Assert.Equal(
            DeleteBehavior.Cascade,
            identifierForeignKey.DeleteBehavior);

        Assert.Contains(
            token.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Single().Name ==
                    nameof(RefreshToken.TokenHash));
    }
}
