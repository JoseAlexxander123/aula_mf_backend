using AulaVirtual.Modules.Courses.Domain.Offerings;

namespace AulaVirtual.Modules.Courses.Application.Offerings;

public sealed class CourseOfferingUseCases(ICourseOfferingRepository repository, TimeProvider timeProvider)
{
    public async Task<CourseOfferingDetails?> CreateCourseOfferingAsync(
        CreateCourseOfferingCommand command, CancellationToken cancellationToken)
    {
        if (!await repository.CourseExistsAsync(command.CourseId, cancellationToken)) return null;
        var offering = CourseOffering.Create(Guid.NewGuid(), command.CourseId, command.Name,
            command.MaximumCapacity, command.BasePrice, command.PlannedSessionCount,
            command.CurrencyCode,
            command.Sessions, timeProvider.GetUtcNow());
        repository.Add(offering);
        await repository.SaveChangesAsync(cancellationToken);
        return CourseOfferingDetails.From(offering);
    }

    public async Task<CourseOfferingDetails?> UpdateCourseOfferingAsync(
        UpdateCourseOfferingCommand command, CancellationToken cancellationToken)
    {
        var offering = await repository.GetAsync(command.OfferingId, cancellationToken);
        if (offering is null || offering.CourseId != command.CourseId) return null;
        offering.Update(command.Name, command.MaximumCapacity, command.BasePrice,
            command.PlannedSessionCount, command.CurrencyCode,
            command.Sessions, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return CourseOfferingDetails.From(offering);
    }

    public async Task<CourseOfferingDetails?> GetCourseOfferingAsync(Guid courseId, Guid offeringId,
        CancellationToken cancellationToken)
    {
        var offering = await repository.GetAsync(offeringId, cancellationToken);
        return offering is null || offering.CourseId != courseId ? null : CourseOfferingDetails.From(offering);
    }

    public async Task<IReadOnlyList<CourseOfferingDetails>?> ListCourseOfferingsByCourseAsync(
        Guid courseId, CancellationToken cancellationToken)
    {
        if (!await repository.CourseExistsAsync(courseId, cancellationToken)) return null;
        var offerings = await repository.ListByCourseAsync(courseId, cancellationToken);
        return offerings.Select(CourseOfferingDetails.From).ToArray();
    }

    public async Task<CourseOfferingDetails?> PublishCourseOfferingAsync(Guid courseId, Guid offeringId,
        CancellationToken cancellationToken)
    {
        var offering = await repository.GetAsync(offeringId, cancellationToken);
        if (offering is null || offering.CourseId != courseId) return null;
        offering.Publish(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return CourseOfferingDetails.From(offering);
    }

    public async Task<CourseOfferingDetails?> UpdateSessionVirtualLinkAsync(Guid courseId, Guid offeringId,
        Guid sessionId, string? virtualLink, CancellationToken cancellationToken)
    {
        var offering = await repository.GetAsync(offeringId, cancellationToken);
        if (offering is null || offering.CourseId != courseId) return null;
        offering.UpdateSessionVirtualLink(sessionId, virtualLink, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return CourseOfferingDetails.From(offering);
    }
}
