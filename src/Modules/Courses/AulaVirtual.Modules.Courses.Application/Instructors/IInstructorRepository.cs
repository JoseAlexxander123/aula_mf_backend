using AulaVirtual.Modules.Courses.Domain.Instructors;

namespace AulaVirtual.Modules.Courses.Application.Instructors;

public interface IInstructorRepository
{
    Task<Instructor?> GetAsync(Guid instructorId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Instructor>> ListAsync(CancellationToken cancellationToken);
    Task<bool> AllExistAsync(IReadOnlyCollection<Guid> instructorIds, CancellationToken cancellationToken);
    Task<bool> UserLinkedElsewhereAsync(Guid userId, Guid instructorId, CancellationToken cancellationToken);
    void Add(Instructor instructor);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
