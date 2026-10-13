namespace AulaVirtual.Modules.Courses.Domain.Offerings;

public sealed class CourseOfferingInstructor
{
    private CourseOfferingInstructor() { }

    private CourseOfferingInstructor(Guid courseOfferingId, Guid instructorId)
    {
        CourseOfferingId = courseOfferingId;
        InstructorId = instructorId;
    }

    public Guid CourseOfferingId { get; private set; }
    public Guid InstructorId { get; private set; }

    internal static CourseOfferingInstructor Create(Guid courseOfferingId, Guid instructorId)
    {
        if (courseOfferingId == Guid.Empty || instructorId == Guid.Empty)
            throw new ArgumentException("Edition and Instructor identifiers are required.");
        return new CourseOfferingInstructor(courseOfferingId, instructorId);
    }
}
