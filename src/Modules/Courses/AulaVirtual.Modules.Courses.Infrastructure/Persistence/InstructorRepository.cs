using AulaVirtual.Modules.Courses.Application.Instructors;
using AulaVirtual.Modules.Courses.Domain.Instructors;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence;

public sealed class InstructorRepository(CoursesDbContext dbContext) : IInstructorRepository
{
    public Task<Instructor?> GetAsync(Guid instructorId, CancellationToken cancellationToken) =>
        dbContext.Instructors.SingleOrDefaultAsync(instructor => instructor.Id == instructorId, cancellationToken);

    public async Task<IReadOnlyList<Instructor>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Instructors.AsNoTracking().OrderBy(instructor => instructor.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> AllExistAsync(IReadOnlyCollection<Guid> instructorIds,
        CancellationToken cancellationToken) =>
        await dbContext.Instructors.CountAsync(instructor => instructorIds.Contains(instructor.Id),
            cancellationToken) == instructorIds.Distinct().Count();

    public Task<bool> UserLinkedElsewhereAsync(Guid userId, Guid instructorId,
        CancellationToken cancellationToken) =>
        dbContext.Instructors.AnyAsync(instructor => instructor.UserId == userId &&
            instructor.Id != instructorId, cancellationToken);

    public void Add(Instructor instructor) => dbContext.Instructors.Add(instructor);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
