using AulaVirtual.Modules.Courses.Domain.Offerings;
using AulaVirtual.SharedKernel.Application.Storage;

namespace AulaVirtual.Modules.Courses.Application.Courses;

public sealed record PublicCourseCatalogRow(Guid CourseId, string Name,
    string? ShortDescription, Guid CategoryId, string CategoryName, string? CoverObjectKey,
    Guid OfferingId, string OfferingName, decimal BasePrice, string CurrencyCode,
    SpecificPromotionType? PromotionType, decimal? PromotionValue,
    DateTimeOffset? PromotionStartsAt, DateTimeOffset? PromotionEndsAt);

public interface IPublicCourseCatalogRepository
{
    Task<IReadOnlyList<PublicCourseCatalogRow>> ListAsync(CancellationToken cancellationToken);
}

public sealed record PublicCourseCategory(Guid Id, string Name);

public sealed record PublicCourseOffering(Guid OfferingId, string Name, decimal BasePrice,
    string CurrencyCode, decimal? PromotionalPrice);

public sealed record PublicCourseCatalogItem(Guid CourseId, string Name,
    string? ShortDescription, PublicCourseCategory Category, string? CoverUrl,
    IReadOnlyList<PublicCourseOffering> Offerings);

public sealed class PublicCourseCatalogUseCases(
    IPublicCourseCatalogRepository repository, TimeProvider timeProvider,
    IFileStorage? storage = null)
{
    public async Task<IReadOnlyList<PublicCourseCatalogItem>> ListAsync(
        CancellationToken cancellationToken)
    {
        var rows = await repository.ListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        return rows.GroupBy(row => row.CourseId).Select(group =>
        {
            var course = group.First();
            return new PublicCourseCatalogItem(course.CourseId, course.Name,
                course.ShortDescription,
                new PublicCourseCategory(course.CategoryId, course.CategoryName),
                CourseCoverUrl.From(course.CoverObjectKey, storage),
                group.Select(offering => new PublicCourseOffering(offering.OfferingId,
                    offering.OfferingName, offering.BasePrice, offering.CurrencyCode,
                    PromotionalPrice(offering, now))).ToArray());
        }).ToArray();
    }

    private static decimal? PromotionalPrice(PublicCourseCatalogRow offering, DateTimeOffset now) =>
        offering.PromotionType is { } type && offering.PromotionValue is { } value &&
        offering.PromotionStartsAt is { } startsAt && offering.PromotionEndsAt is { } endsAt &&
        SpecificPromotion.IsActive(now, startsAt, endsAt)
            ? SpecificPromotion.CalculatePrice(type, value, offering.BasePrice)
            : null;
}
