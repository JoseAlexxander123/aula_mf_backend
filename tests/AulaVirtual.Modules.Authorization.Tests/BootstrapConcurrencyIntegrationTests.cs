using AulaVirtual.Modules.Authorization.Application;
using AulaVirtual.Modules.Authorization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AulaVirtual.Modules.Authorization.Tests;

public sealed class BootstrapConcurrencyIntegrationTests
{
    [Fact]
    public async Task ConcurrentConnectionsAgainstPostgresCreateOnlyOneInitialAdministrator()
    {
        // Opt in against the local development database. Only test-created UserRoles are removed.
        var source = Environment.GetEnvironmentVariable("AUTHORIZATION_BOOTSTRAP_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(source)) return;
        var builder = new NpgsqlConnectionStringBuilder(source);
        if (builder.Host is not ("localhost" or "127.0.0.1" or "::1") ||
            builder.Database != "aula_virtual_dev")
            throw new InvalidOperationException("Bootstrap integration test requires local aula_virtual_dev.");

        var options = new DbContextOptionsBuilder<AuthorizationDbContext>()
            .UseNpgsql(builder.ConnectionString).Options;
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        Guid roleId;
        int authSessionsBefore;
        await using (var setup = new AuthorizationDbContext(options))
        {
            var role = await setup.Roles.SingleAsync(x => x.Code == Roles.Administrator);
            roleId = role.Id;
            if (await setup.UserRoles.AnyAsync(x => x.RoleId == roleId))
                throw new InvalidOperationException("Local development database already has an administrator; use an empty local test database.");
            authSessionsBefore = await setup.Database.SqlQueryRaw<int>(
                "SELECT count(*)::integer AS \"Value\" FROM \"auth\".\"auth_sessions\"").SingleAsync();
        }

        try
        {
            var users = new AuthorizationTests.FakeUsers();
            users.Existing.UnionWith([first, second]);
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<bool> Attempt(Guid userId)
            {
                await start.Task;
                await using var context = new AuthorizationDbContext(options);
                var repository = new AuthorizationRepository(context);
                var useCases = new AuthorizationUseCases(repository, users, new PermissionChecker(repository));
                return await useCases.AssignFirstAdministratorAsync(userId, default);
            }
            var attempts = new[] { Task.Run(() => Attempt(first)), Task.Run(() => Attempt(second)) };
            start.SetResult();
            var outcomes = await Task.WhenAll(attempts);
            Assert.Single(outcomes, succeeded => succeeded);

            await using var verify = new AuthorizationDbContext(options);
            var assignments = await verify.UserRoles.Where(x => x.RoleId == roleId).ToListAsync();
            var assigned = Assert.Single(assignments).UserId;
            Assert.Contains(assigned, new[] { first, second });
            await using (var retryContext = new AuthorizationDbContext(options))
            {
                var retryRepository = new AuthorizationRepository(retryContext);
                var retry = new AuthorizationUseCases(retryRepository, users, new PermissionChecker(retryRepository));
                Assert.False(await retry.AssignFirstAdministratorAsync(assigned, default));
            }
            Assert.Single(await verify.UserRoles.Where(x => x.RoleId == roleId).ToListAsync());

            // The special bootstrap does not constrain later normal assignments.
            var other = assigned == first ? second : first;
            await using (var normalContext = new AuthorizationDbContext(options))
            {
                var normalRepository = new AuthorizationRepository(normalContext);
                var normal = new AuthorizationUseCases(normalRepository, users, new PermissionChecker(normalRepository));
                Assert.True(await normal.AssignRoleAsync(other, Roles.Administrator, default));
            }
            Assert.Equal(2, await verify.UserRoles.CountAsync(x => x.RoleId == roleId));
            Assert.Equal(authSessionsBefore, await verify.Database.SqlQueryRaw<int>(
                "SELECT count(*)::integer AS \"Value\" FROM \"auth\".\"auth_sessions\"").SingleAsync());
        }
        finally
        {
            await using var cleanup = new AuthorizationDbContext(options);
            await cleanup.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"authorization\".\"user_roles\" WHERE role_id = {roleId} AND user_id IN ({first}, {second})");
        }
    }
}
