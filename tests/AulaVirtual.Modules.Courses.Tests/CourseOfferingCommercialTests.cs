using AulaVirtual.Modules.Courses.Application.Instructors;
using AulaVirtual.Modules.Courses.Application.Offerings;
using AulaVirtual.Modules.Courses.Domain.Instructors;
using AulaVirtual.Modules.Courses.Domain.Offerings;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class CourseOfferingCommercialTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 1, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CurrencyCodeRequiresStableUppercaseFormat()
    {
        Assert.Equal("USD", NewOffering("USD").CurrencyCode);
        foreach (var invalid in new[] { "PEN ", "soles", "usd", "US1", "" })
            Assert.Throws<ArgumentException>(() => NewOffering(invalid));
    }

    [Fact]
    public void EditionSupportsManyInstructorsAndInstructorCanBeReused()
    {
        var first = NewOffering("PEN");
        var second = NewOffering("USD");
        var sharedInstructor = Guid.NewGuid();
        var otherInstructor = Guid.NewGuid();

        first.AssignInstructors([sharedInstructor, otherInstructor], Start);
        second.AssignInstructors([sharedInstructor], Start);
        Assert.Equal(2, first.InstructorIds.Count);
        Assert.Single(second.InstructorIds);
        Assert.Contains(sharedInstructor, first.InstructorIds);
        Assert.Contains(sharedInstructor, second.InstructorIds);
        Assert.Throws<ArgumentException>(() => first.AssignInstructors([], Start));
        Assert.Throws<ArgumentException>(() => first.AssignInstructors([sharedInstructor, sharedInstructor], Start));
        first.AssignInstructors([otherInstructor], Start);
        Assert.Equal([otherInstructor], first.InstructorIds);
    }

    [Fact]
    public void PublicationRequiresAnInstructor()
    {
        var offering = NewOffering("PEN");
        Assert.Throws<InvalidOperationException>(() => offering.Publish(Start));
        offering.AssignInstructors([Guid.NewGuid()], Start);
        offering.Publish(Start);
        Assert.Equal(CourseOfferingStatus.Published, offering.Status);
    }

    [Fact]
    public async Task CommercialUseCasesCheckInstructorExistenceAndReturnPricePreview()
    {
        var offering = NewOffering("USD");
        var instructor = Instructor.Create(Guid.NewGuid(), Start);
        var offerings = new FakeOfferings(offering);
        var instructors = new FakeInstructors(instructor);
        var useCases = new CourseOfferingCommercialUseCases(offerings, instructors, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() => useCases.AssignInstructorsAsync(
            offering.CourseId, offering.Id, [Guid.NewGuid()], default));
        var assigned = await useCases.AssignInstructorsAsync(offering.CourseId, offering.Id,
            [instructor.Id], default);
        Assert.Equal([instructor.Id], assigned!.InstructorIds);

        var price = await useCases.GetSpecificPricePreviewAsync(offering.CourseId, offering.Id, default);
        Assert.Equal(100m, price!.EffectivePrice);
        Assert.Equal(PriceSource.Base, price.Source);
        Assert.Equal(1, offerings.SaveCount);
    }

    [Fact]
    public void PercentagePromotionWinsOverGlobalAndNeverAccumulates()
    {
        var offering = NewOffering("USD");
        offering.SetSpecificPromotion(SpecificPromotionType.Percentage, 25, Start, Start.AddDays(2), Start);
        var active = offering.ResolvePrice(Start.AddDays(1), hasActiveGlobalCampaign: true);
        Assert.Equal(PriceSource.SpecificPromotion, active.Source);
        Assert.Equal(75m, active.EffectivePrice);

        var expired = offering.ResolvePrice(Start.AddDays(2), hasActiveGlobalCampaign: true);
        Assert.Equal(PriceSource.GlobalCampaignPending, expired.Source);
        Assert.Null(expired.EffectivePrice);
        Assert.Equal(PriceSource.Base, offering.ResolvePrice(Start.AddDays(2)).Source);
    }

    [Fact]
    public void FixedAmountUsesEditionCurrencyAndClampsPriceAtZero()
    {
        var offering = NewOffering("EUR");
        offering.SetSpecificPromotion(SpecificPromotionType.FixedAmount, 150, Start, Start.AddDays(2), Start);
        var quote = offering.ResolvePrice(Start.AddDays(1));
        Assert.Equal("EUR", quote.CurrencyCode);
        Assert.Equal(0m, quote.EffectivePrice);
        Assert.Throws<InvalidOperationException>(() => offering.SetCurrencyCode("USD", Start));

        offering.RemoveSpecificPromotion(Start);
        offering.SetCurrencyCode("USD", Start);
        Assert.Equal("USD", offering.ResolvePrice(Start).CurrencyCode);
    }

    [Fact]
    public void InvalidPromotionIsRejected()
    {
        var offering = NewOffering("PEN");
        Assert.Throws<ArgumentOutOfRangeException>(() => offering.SetSpecificPromotion(
            SpecificPromotionType.Percentage, 101, Start, Start.AddDays(1), Start));
        Assert.Throws<ArgumentException>(() => offering.SetSpecificPromotion(
            SpecificPromotionType.FixedAmount, 10, Start, Start, Start));
    }

    private static CourseOffering NewOffering(string currencyCode) => CourseOffering.Create(
        Guid.NewGuid(), Guid.NewGuid(), "Edition", 20, 100m, 1, currencyCode,
        [new SessionPlan(null, Start, Start.AddHours(2), null)], Start);

    private sealed class FakeOfferings(CourseOffering offering) : ICourseOfferingRepository
    {
        public int SaveCount { get; private set; }
        public Task<bool> CourseExistsAsync(Guid courseId, CancellationToken cancellationToken) =>
            Task.FromResult(courseId == offering.CourseId);
        public Task<CourseOffering?> GetAsync(Guid offeringId, CancellationToken cancellationToken) =>
            Task.FromResult<CourseOffering?>(offeringId == offering.Id ? offering : null);
        public Task<IReadOnlyList<CourseOffering>> ListByCourseAsync(Guid courseId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CourseOffering>>([offering]);
        public Task<IReadOnlyList<PublishedOfferingSummary>> ListPubliclyVisibleByCourseAsync(
            Guid courseId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PublishedOfferingSummary>>([]);
        public void Add(CourseOffering item) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInstructors(Instructor instructor) : IInstructorRepository
    {
        public Task<Instructor?> GetAsync(Guid instructorId, CancellationToken cancellationToken) =>
            Task.FromResult<Instructor?>(instructorId == instructor.Id ? instructor : null);
        public Task<IReadOnlyList<Instructor>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Instructor>>([instructor]);
        public Task<bool> AllExistAsync(IReadOnlyCollection<Guid> instructorIds, CancellationToken cancellationToken) =>
            Task.FromResult(instructorIds.All(id => id == instructor.Id));
        public Task<bool> UserLinkedElsewhereAsync(Guid userId, Guid instructorId, CancellationToken cancellationToken) =>
            Task.FromResult(false);
        public void Add(Instructor item) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
