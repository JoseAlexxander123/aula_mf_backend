using AulaVirtual.Modules.Courses.Application.Courses;
using AulaVirtual.Modules.Courses.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence;

public sealed class CourseRepository(CoursesDbContext dbContext) : ICourseRepository
{
    public Task<Course?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Courses.SingleOrDefaultAsync(course => course.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Course>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Courses.AsNoTracking().OrderBy(course => course.Title)
            .ThenBy(course => course.Id).ToListAsync(cancellationToken);

    public Task<bool> CategoryExistsAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(category => category.Id == id, cancellationToken);

    public Task<CourseCategory?> GetCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CourseCategory>> ListCategoriesAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories.AsNoTracking().OrderBy(category => category.Name)
            .ThenBy(category => category.Id).ToListAsync(cancellationToken);

    public Task<bool> CategoryHasCoursesAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Courses.AnyAsync(course => course.CategoryId == id, cancellationToken);

    public void AddCategory(CourseCategory category) => dbContext.Categories.Add(category);

    public async Task<bool> DeleteCategoryAsync(CourseCategory category, CancellationToken cancellationToken)
    {
        dbContext.Categories.Remove(category);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException exception) when (IsCategoryReferenceViolation(exception))
        {
            throw new CourseCategoryInUseException();
        }
    }

    internal static bool IsCategoryReferenceViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { ConstraintName: "fk_courses_category_id" } postgres &&
        postgres.SqlState is PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.RestrictViolation;

    public void Add(Course course) => dbContext.Courses.Add(course);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
