using AulaVirtual.Modules.Courses.Domain.Offerings;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class CourseOfferingTests
{
    private static readonly DateTimeOffset First = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Second = First.AddDays(2);

    [Fact]
    public void Create_RequiresCourseAndStoresFinalSessionsAsDraft()
    {
        Assert.Throws<ArgumentException>(() => CourseOffering.Create(Guid.NewGuid(), Guid.Empty,
            "Edition", 20, 100, 1, "USD", [new SessionPlan(null, First, First.AddHours(2), null)], First));

        var offering = NewOffering();
        Assert.Equal(CourseOfferingStatus.Draft, offering.Status);
        Assert.Equal(20, offering.MaximumCapacity);
        Assert.Equal(100, offering.BasePrice);
        Assert.Equal(2, offering.Sessions.Count);
        Assert.All(offering.Sessions, session => Assert.Equal(offering.Id, session.CourseOfferingId));
    }

    [Fact]
    public void Create_RejectsInvalidCalendar()
    {
        Assert.Throws<ArgumentException>(() => CourseOffering.Create(Guid.NewGuid(), Guid.NewGuid(),
            "Edition", 20, 100, 2, "USD", [new SessionPlan(null, First, First.AddHours(2), null)], First));
        Assert.Throws<ArgumentException>(() => CourseOffering.Create(Guid.NewGuid(), Guid.NewGuid(),
            "Edition", 20, 100, 1, "USD", [new SessionPlan(null, First, First, null)], First));
        Assert.Throws<ArgumentException>(() => CourseOffering.Create(Guid.NewGuid(), Guid.NewGuid(),
            "Edition", 20, 100, 1, "USD",
            [new SessionPlan(null, First.ToOffset(TimeSpan.FromHours(-5)), First.AddHours(2), null)], First));
    }

    [Fact]
    public void DraftCanBeEdited_ButPublishedCalendarCannotBeReplaced()
    {
        var offering = NewOffering();
        var originalId = offering.Sessions[0].Id;
        offering.Update("  New name  ", 25, 120, 2, "USD",
            [new SessionPlan(originalId, First.AddHours(1), First.AddHours(3), "https://example.org/live"),
             new SessionPlan(offering.Sessions[1].Id, Second, Second.AddHours(2), null)], First);

        Assert.Equal("New name", offering.Name);
        Assert.Equal(originalId, offering.Sessions[0].Id);
        Assert.Equal(First.AddHours(1), offering.Sessions[0].StartsAt);
        Assert.Equal("https://example.org/live", offering.Sessions[0].VirtualLink);

        offering.AssignInstructors([Guid.NewGuid()], First);
        offering.Publish(First);
        Assert.Equal(CourseOfferingStatus.Published, offering.Status);
        Assert.Throws<InvalidOperationException>(() => offering.Update("Again", 25, 120, 2, "USD",
            [new SessionPlan(originalId, First, First.AddHours(2), null),
             new SessionPlan(offering.Sessions[1].Id, Second, Second.AddHours(2), null)], First));
        offering.UpdateSessionVirtualLink(originalId, "https://example.org/new", First);
        Assert.Equal("https://example.org/new", offering.Sessions[0].VirtualLink);
    }

    [Fact]
    public void InvalidDraftEdit_DoesNotPartiallyChangeSessions()
    {
        var offering = NewOffering();
        var originalId = offering.Sessions[0].Id;
        Assert.Throws<ArgumentException>(() => offering.Update("Changed", 25, 120, 2, "USD",
            [new SessionPlan(originalId, First.AddHours(1), First.AddHours(3), null),
             new SessionPlan(offering.Sessions[1].Id, Second, Second, null)], First));
        Assert.Equal(First, offering.Sessions[0].StartsAt);
        Assert.Equal("Edition", offering.Name);
    }

    private static CourseOffering NewOffering() => CourseOffering.Create(Guid.NewGuid(), Guid.NewGuid(),
        "Edition", 20, 100, 2, "USD",
        [new SessionPlan(null, First, First.AddHours(2), null),
         new SessionPlan(null, Second, Second.AddHours(2), null)], First);
}
