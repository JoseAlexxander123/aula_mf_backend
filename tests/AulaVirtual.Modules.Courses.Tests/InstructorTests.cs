using AulaVirtual.Modules.Courses.Application.Instructors;
using AulaVirtual.Modules.Courses.Domain.Instructors;
using AulaVirtual.Modules.Users.Application.Abstractions;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class InstructorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void InstructorExistsWithoutUserAndKeepsItsIdentityWhenLinked()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var instructor = Instructor.Create(id, Now);
        Assert.Null(instructor.UserId);

        instructor.LinkUser(userId, Now);
        Assert.Equal(id, instructor.Id);
        Assert.Equal(userId, instructor.UserId);
        instructor.LinkUser(userId, Now);
        Assert.Throws<InvalidOperationException>(() => instructor.LinkUser(Guid.NewGuid(), Now));
    }

    [Fact]
    public async Task LinkUserChecksExistenceAndRejectsDuplicateInstructorIdentity()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeInstructors();
        var users = new FakeUsers(userId);
        var useCases = new InstructorUseCases(repository, users, TimeProvider.System);
        var first = await useCases.CreateInstructorAsync(default);
        var second = await useCases.CreateInstructorAsync(default);
        Assert.Null(first.UserId);
        Assert.Equal(2, (await useCases.ListInstructorsAsync(default)).Count);

        Assert.Equal(LinkUserStatus.UserNotFound,
            (await useCases.LinkUserAsync(first.Id, Guid.NewGuid(), default)).Status);
        var linked = await useCases.LinkUserAsync(first.Id, userId, default);
        Assert.Equal(LinkUserStatus.Linked, linked.Status);
        Assert.Equal(first.Id, linked.Instructor!.Id);
        Assert.Equal(userId, (await useCases.GetInstructorAsync(first.Id, default))!.UserId);
        Assert.Equal(LinkUserStatus.UserAlreadyLinked,
            (await useCases.LinkUserAsync(second.Id, userId, default)).Status);
    }

    private sealed class FakeUsers(Guid knownUserId) : IUserExistence
    {
        public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(userId == knownUserId);
    }

    private sealed class FakeInstructors : IInstructorRepository
    {
        private readonly List<Instructor> _items = [];
        public Task<Instructor?> GetAsync(Guid instructorId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.SingleOrDefault(item => item.Id == instructorId));
        public Task<IReadOnlyList<Instructor>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Instructor>>(_items.ToArray());
        public Task<bool> AllExistAsync(IReadOnlyCollection<Guid> instructorIds, CancellationToken cancellationToken) =>
            Task.FromResult(instructorIds.All(id => _items.Any(item => item.Id == id)));
        public Task<bool> UserLinkedElsewhereAsync(Guid userId, Guid instructorId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Any(item => item.Id != instructorId && item.UserId == userId));
        public void Add(Instructor instructor) => _items.Add(instructor);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
