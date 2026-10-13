using AulaVirtual.Modules.Courses.Domain.Instructors;
using AulaVirtual.Modules.Users.Application.Abstractions;

namespace AulaVirtual.Modules.Courses.Application.Instructors;

public sealed record InstructorDetails(Guid Id, Guid? UserId)
{
    public static InstructorDetails From(Instructor instructor) => new(instructor.Id, instructor.UserId);
}

public enum LinkUserStatus { Linked, InstructorNotFound, UserNotFound, UserAlreadyLinked }

public sealed record LinkUserResult(LinkUserStatus Status, InstructorDetails? Instructor = null);

public sealed class InstructorUseCases(
    IInstructorRepository instructors,
    IUserExistence users,
    TimeProvider timeProvider)
{
    public async Task<InstructorDetails> CreateInstructorAsync(CancellationToken cancellationToken)
    {
        var instructor = Instructor.Create(Guid.NewGuid(), timeProvider.GetUtcNow());
        instructors.Add(instructor);
        await instructors.SaveChangesAsync(cancellationToken);
        return InstructorDetails.From(instructor);
    }

    public async Task<InstructorDetails?> GetInstructorAsync(Guid instructorId, CancellationToken cancellationToken)
    {
        var instructor = await instructors.GetAsync(instructorId, cancellationToken);
        return instructor is null ? null : InstructorDetails.From(instructor);
    }

    public async Task<IReadOnlyList<InstructorDetails>> ListInstructorsAsync(CancellationToken cancellationToken) =>
        (await instructors.ListAsync(cancellationToken)).Select(InstructorDetails.From).ToArray();

    public async Task<LinkUserResult> LinkUserAsync(Guid instructorId, Guid userId,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
        var instructor = await instructors.GetAsync(instructorId, cancellationToken);
        if (instructor is null) return new LinkUserResult(LinkUserStatus.InstructorNotFound);
        if (!await users.ExistsAsync(userId, cancellationToken))
            return new LinkUserResult(LinkUserStatus.UserNotFound);
        if (await instructors.UserLinkedElsewhereAsync(userId, instructorId, cancellationToken))
            return new LinkUserResult(LinkUserStatus.UserAlreadyLinked);
        instructor.LinkUser(userId, timeProvider.GetUtcNow());
        await instructors.SaveChangesAsync(cancellationToken);
        return new LinkUserResult(LinkUserStatus.Linked, InstructorDetails.From(instructor));
    }
}
