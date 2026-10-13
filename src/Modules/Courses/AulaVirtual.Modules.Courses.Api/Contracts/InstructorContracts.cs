using AulaVirtual.Modules.Courses.Application.Instructors;

namespace AulaVirtual.Modules.Courses.Api.Contracts;

public sealed record LinkInstructorUserRequest(Guid UserId);

public sealed record InstructorResponse(Guid Id, Guid? UserId)
{
    public static InstructorResponse From(InstructorDetails details) => new(details.Id, details.UserId);
}
