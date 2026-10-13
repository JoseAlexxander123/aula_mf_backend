using AulaVirtual.Modules.Users.Application.Abstractions;
using AulaVirtual.Modules.Users.Application.Users.CreateUser;
using AulaVirtual.Modules.Users.Domain.Users;

namespace AulaVirtual.Modules.Users.Tests;

public sealed class CreateUserTests
{
    [Fact]
    public async Task CreateUser_TrimsValuesAndReturnsUserId()
    {
        var repository = new FakeUserRepository();
        var handler = new CreateUserHandler(
            repository,
            new CreateUserCommandValidator(),
            TimeProvider.System);

        var result = await handler.ExecuteAsync(
            new CreateUserCommand(
                "  Ana Maria  ",
                "  Gamboa ",
                null,
                " ana@example.com ",
                " DNI ",
                " 77340061 "),
            CancellationToken.None);

        Assert.Equal(CreateUserStatus.Created, result.Status);
        Assert.NotNull(result.UserId);
        var user = Assert.Single(repository.Users);
        Assert.Equal("Ana Maria", user.GivenNames);
        Assert.Equal("Gamboa", user.FirstSurname);
        Assert.Equal("ana@example.com", user.Email);
        Assert.Equal("DNI", user.DocumentType);
        Assert.Equal("77340061", user.DocumentNumber);
    }

    [Fact]
    public async Task CreateUser_RejectsDuplicateDocument()
    {
        var repository = new FakeUserRepository
        {
            DuplicateDocument = true
        };
        var handler = new CreateUserHandler(
            repository,
            new CreateUserCommandValidator(),
            TimeProvider.System);

        var result = await handler.ExecuteAsync(
            new CreateUserCommand(
                "Ana",
                "Gamboa",
                null,
                null,
                "DNI",
                "77340061"),
            CancellationToken.None);

        Assert.Equal(
            CreateUserStatus.DuplicateDocument,
            result.Status);
        Assert.Empty(repository.Users);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public bool DuplicateDocument { get; init; }

        public List<User> Users { get; } = [];

        public Task<bool> DocumentNumberExistsAsync(
            string documentNumber,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(DuplicateDocument);
        }

        public void Add(User user)
        {
            Users.Add(user);
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
