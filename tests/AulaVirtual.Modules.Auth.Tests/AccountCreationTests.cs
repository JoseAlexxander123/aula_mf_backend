using AulaVirtual.Modules.Auth.Application.Abstractions;
using AulaVirtual.Modules.Auth.Application.Accounts.CreateAccount;
using AulaVirtual.Modules.Auth.Domain.Accounts;
using AulaVirtual.Modules.Users.Application.Users.CreateUser;

namespace AulaVirtual.Modules.Auth.Tests;

public sealed class AccountCreationTests
{
    [Fact]
    public async Task StudentCreation_CreatesUserBeforeAuthAccount()
    {
        var calls = new List<string>();
        var userId = Guid.NewGuid();
        var createUser = new StubCreateUser(userId, calls);
        var repository = new TrackingAuthRepository(calls);
        var handler = new CreateStudentAccountHandler(
            createUser,
            repository,
            new FixedPasswordGenerator(),
            new StubPasswordHasher("irrelevant"),
            new CreateStudentAccountCommandValidator(),
            TimeProvider.System);

        var result = await handler.ExecuteAsync(
            new CreateStudentAccountCommand(
                "Ana",
                "Gamboa",
                null,
                null,
                "DNI",
                "77340061"),
            CancellationToken.None);

        Assert.Equal(CreateAccountStatus.Created, result.Status);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("G77340061", result.LoginIdentifier);
        Assert.Equal("Aa2!password", result.InitialPassword);
        Assert.Equal(["users.create", "auth.add", "auth.save"], calls);
        var account = Assert.Single(repository.Accounts);
        var identifier = Assert.Single(account.Identifiers);
        Assert.Equal(userId, account.UserId);
        Assert.Equal(LoginType.Username, identifier.LoginType);
    }

    [Fact]
    public async Task AdministratorCreation_UsesEmailWithoutGrantingAuthorizationData()
    {
        var repository = new TrackingAuthRepository([]);
        var handler = new CreateAdministratorAccountHandler(
            new StubCreateUser(Guid.NewGuid(), []),
            repository,
            new FixedPasswordGenerator(),
            new StubPasswordHasher("irrelevant"),
            new CreateAdministratorAccountCommandValidator(),
            TimeProvider.System);

        var result = await handler.ExecuteAsync(
            new CreateAdministratorAccountCommand(
                "Ada",
                "Lovelace",
                null,
                " Admin@Empresa.com ",
                null,
                null),
            CancellationToken.None);

        Assert.Equal(CreateAccountStatus.Created, result.Status);
        var account = Assert.Single(repository.Accounts);
        var identifier = Assert.Single(account.Identifiers);
        Assert.Equal(LoginType.Email, identifier.LoginType);
        Assert.Equal(
            "ADMIN@EMPRESA.COM",
            identifier.NormalizedLoginIdentifier);
        Assert.DoesNotContain(
            account.GetType().GetProperties(),
            property =>
                property.Name.Contains(
                    "Role",
                    StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StubCreateUser(
        Guid userId,
        List<string> calls) : ICreateUser
    {
        public Task<CreateUserResult> ExecuteAsync(
            CreateUserCommand command,
            CancellationToken cancellationToken)
        {
            calls.Add("users.create");

            return Task.FromResult(
                new CreateUserResult(
                    CreateUserStatus.Created,
                    userId));
        }
    }

    private sealed class FixedPasswordGenerator : IPasswordGenerator
    {
        public string Generate() => "Aa2!password";
    }

    private sealed class TrackingAuthRepository(List<string> calls)
        : FakeAuthRepository
    {
        public override void AddAccount(AuthAccount account)
        {
            calls.Add("auth.add");
            base.AddAccount(account);
        }

        public override Task SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            calls.Add("auth.save");
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
