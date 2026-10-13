using AulaVirtual.Modules.Courses.Domain.Courses;

namespace AulaVirtual.Modules.Courses.Application.Courses;

public interface ICourseRepository
{
    Task<Course?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Course>> ListAsync(CancellationToken cancellationToken);
    Task<bool> CategoryExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<CourseCategory?> GetCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CourseCategory>> ListCategoriesAsync(CancellationToken cancellationToken);
    Task<bool> CategoryHasCoursesAsync(Guid id, CancellationToken cancellationToken);
    void AddCategory(CourseCategory category);
    Task<bool> DeleteCategoryAsync(CourseCategory category, CancellationToken cancellationToken);
    void Add(Course course);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
