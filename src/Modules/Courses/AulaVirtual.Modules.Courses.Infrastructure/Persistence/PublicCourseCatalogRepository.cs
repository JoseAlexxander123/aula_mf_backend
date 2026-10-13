using AulaVirtual.Modules.Courses.Application.Courses;
using AulaVirtual.Modules.Courses.Domain.Courses;
using AulaVirtual.Modules.Courses.Domain.Offerings;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence;

public sealed class PublicCourseCatalogRepository(CoursesDbContext dbContext)
    : IPublicCourseCatalogRepository
{
    public async Task<IReadOnlyList<PublicCourseCatalogRow>> ListAsync(
        CancellationToken cancellationToken) =>
        await BuildQuery(dbContext.Courses.AsNoTracking(),
            dbContext.Categories.AsNoTracking(),
            dbContext.CourseOfferings.AsNoTracking()).ToListAsync(cancellationToken);

    internal static IQueryable<PublicCourseCatalogRow> BuildQuery(
        IQueryable<Course> courses, IQueryable<CourseCategory> categories,
        IQueryable<CourseOffering> offerings) =>
        from course in courses
        join category in categories on course.CategoryId equals (Guid?)category.Id
        join offering in offerings on course.Id equals offering.CourseId
        where course.IsPublished && offering.Status == CourseOfferingStatus.Published &&
              offering.CurrencyCode != null
        orderby course.Title, course.Id, offering.CreatedAt, offering.Id
        select new PublicCourseCatalogRow(course.Id, course.Title,
            course.ShortDescription, category.Id, category.Name, course.CoverObjectKey,
            offering.Id, offering.Name, offering.BasePrice, offering.CurrencyCode!,
            offering.SpecificPromotion == null ? null : offering.SpecificPromotion.Type,
            offering.SpecificPromotion == null ? null : offering.SpecificPromotion.Value,
            offering.SpecificPromotion == null ? null : offering.SpecificPromotion.StartsAt,
            offering.SpecificPromotion == null ? null : offering.SpecificPromotion.EndsAt);
}
