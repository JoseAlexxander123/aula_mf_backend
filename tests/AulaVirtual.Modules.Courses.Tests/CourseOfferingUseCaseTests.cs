using AulaVirtual.Modules.Courses.Application.Offerings;
using AulaVirtual.Modules.Courses.Domain.Offerings;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class CourseOfferingUseCaseTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateGetListUpdateAndPublish_StayWithinCourse()
    {
        var courseId = Guid.NewGuid();
        var repository = new FakeRepository(courseId);
        var useCases = new CourseOfferingUseCases(repository, TimeProvider.System);
        var sessions = new[] { new SessionPlan(null, Start, Start.AddHours(2), null) };

        Assert.Null(await useCases.CreateCourseOfferingAsync(
            new CreateCourseOfferingCommand(Guid.NewGuid(), "Edition", 20, 100, "USD", 1, sessions), default));

        var created = await useCases.CreateCourseOfferingAsync(
            new CreateCourseOfferingCommand(courseId, "Edition", 20, 100, "USD", 1, sessions), default);
        Assert.NotNull(created);
        Assert.Equal(CourseOfferingStatus.Draft, created.Status);
        Assert.Equal(created.Id, (await useCases.GetCourseOfferingAsync(courseId, created.Id, default))!.Id);
        Assert.Null(await useCases.GetCourseOfferingAsync(Guid.NewGuid(), created.Id, default));
        Assert.Single((await useCases.ListCourseOfferingsByCourseAsync(courseId, default))!);

        var updated = await useCases.UpdateCourseOfferingAsync(new UpdateCourseOfferingCommand(courseId,
            created.Id, "Updated", 25, 120, "USD", 1,
            [new SessionPlan(created.Sessions[0].Id, Start.AddDays(1), Start.AddDays(1).AddHours(2), null)]), default);
        Assert.Equal("Updated", updated!.Name);
        Assert.Equal(created.Sessions[0].Id, updated.Sessions[0].Id);

        repository.GetStored(created.Id)!.AssignInstructors([Guid.NewGuid()], Start);
        var published = await useCases.PublishCourseOfferingAsync(courseId, created.Id, default);
        Assert.Equal(CourseOfferingStatus.Published, published!.Status);
        Assert.True(repository.SaveCount >= 3);
    }

    private sealed class FakeRepository(Guid knownCourseId) : ICourseOfferingRepository
    {
        private readonly List<CourseOffering> _offerings = [];
        public int SaveCount { get; private set; }
        public CourseOffering? GetStored(Guid id) => _offerings.SingleOrDefault(offering => offering.Id == id);
        public Task<bool> CourseExistsAsync(Guid courseId, CancellationToken cancellationToken) =>
            Task.FromResult(courseId == knownCourseId);
        public Task<CourseOffering?> GetAsync(Guid offeringId, CancellationToken cancellationToken) =>
            Task.FromResult(_offerings.SingleOrDefault(offering => offering.Id == offeringId));
        public Task<IReadOnlyList<CourseOffering>> ListByCourseAsync(Guid courseId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CourseOffering>>(_offerings.Where(offering => offering.CourseId == courseId).ToArray());
        public Task<IReadOnlyList<PublishedOfferingSummary>> ListPubliclyVisibleByCourseAsync(
            Guid courseId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PublishedOfferingSummary>>([]);
        public void Add(CourseOffering offering) => _offerings.Add(offering);
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
