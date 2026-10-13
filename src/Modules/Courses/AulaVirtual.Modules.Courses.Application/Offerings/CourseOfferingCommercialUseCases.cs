using AulaVirtual.Modules.Courses.Application.Instructors;
using AulaVirtual.Modules.Courses.Domain.Offerings;

namespace AulaVirtual.Modules.Courses.Application.Offerings;

public sealed class CourseOfferingCommercialUseCases(
    ICourseOfferingRepository offerings,
    IInstructorRepository instructors,
    TimeProvider timeProvider)
{
    public async Task<CourseOfferingDetails?> AssignInstructorsAsync(Guid courseId, Guid offeringId,
        IReadOnlyList<Guid> instructorIds, CancellationToken cancellationToken)
    {
        var offering = await GetOfferingAsync(courseId, offeringId, cancellationToken);
        if (offering is null) return null;
        ArgumentNullException.ThrowIfNull(instructorIds);
        if (instructorIds.Count == 0 || !await instructors.AllExistAsync(instructorIds, cancellationToken))
            throw new ArgumentException("At least one existing Instructor is required.", nameof(instructorIds));
        offering.AssignInstructors(instructorIds, timeProvider.GetUtcNow());
        await offerings.SaveChangesAsync(cancellationToken);
        return CourseOfferingDetails.From(offering);
    }

    public async Task<CourseOfferingDetails?> UpdateCurrencyCodeAsync(Guid courseId, Guid offeringId,
        string currencyCode, CancellationToken cancellationToken)
    {
        var offering = await GetOfferingAsync(courseId, offeringId, cancellationToken);
        if (offering is null) return null;
        offering.SetCurrencyCode(currencyCode, timeProvider.GetUtcNow());
        await offerings.SaveChangesAsync(cancellationToken);
        return CourseOfferingDetails.From(offering);
    }

    public async Task<CourseOfferingDetails?> SetSpecificPromotionAsync(Guid courseId, Guid offeringId,
        SpecificPromotionType type, decimal value, DateTimeOffset startsAt, DateTimeOffset endsAt,
        CancellationToken cancellationToken)
    {
        var offering = await GetOfferingAsync(courseId, offeringId, cancellationToken);
        if (offering is null) return null;
        offering.SetSpecificPromotion(type, value, startsAt, endsAt, timeProvider.GetUtcNow());
        await offerings.SaveChangesAsync(cancellationToken);
        return CourseOfferingDetails.From(offering);
    }

    public async Task<CourseOfferingDetails?> RemoveSpecificPromotionAsync(Guid courseId, Guid offeringId,
        CancellationToken cancellationToken)
    {
        var offering = await GetOfferingAsync(courseId, offeringId, cancellationToken);
        if (offering is null) return null;
        offering.RemoveSpecificPromotion(timeProvider.GetUtcNow());
        await offerings.SaveChangesAsync(cancellationToken);
        return CourseOfferingDetails.From(offering);
    }

    // Global campaign pricing is deliberately not evaluated until its model is defined.
    public async Task<PriceQuote?> GetSpecificPricePreviewAsync(Guid courseId, Guid offeringId,
        CancellationToken cancellationToken)
    {
        var offering = await GetOfferingAsync(courseId, offeringId, cancellationToken);
        return offering?.ResolvePrice(timeProvider.GetUtcNow());
    }

    private async Task<CourseOffering?> GetOfferingAsync(Guid courseId, Guid offeringId,
        CancellationToken cancellationToken)
    {
        var offering = await offerings.GetAsync(offeringId, cancellationToken);
        return offering is null || offering.CourseId != courseId ? null : offering;
    }
}
