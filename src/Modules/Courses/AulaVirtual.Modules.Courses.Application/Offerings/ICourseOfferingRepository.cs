using AulaVirtual.Modules.Courses.Domain.Offerings;

namespace AulaVirtual.Modules.Courses.Application.Offerings;

public interface ICourseOfferingRepository
{
    Task<bool> CourseExistsAsync(Guid courseId, CancellationToken cancellationToken);
    Task<CourseOffering?> GetAsync(Guid offeringId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseOffering>> ListByCourseAsync(Guid courseId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PublishedOfferingSummary>> ListPubliclyVisibleByCourseAsync(Guid courseId, CancellationToken cancellationToken);
    void Add(CourseOffering offering);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record PublishedOfferingSummary(Guid Id, Guid CourseId, string Name,
    decimal BasePrice, string? CurrencyCode);
