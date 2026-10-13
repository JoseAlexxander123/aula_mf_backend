namespace AulaVirtual.Modules.Courses.Domain.Instructors;

public sealed class Instructor
{
    private Instructor() { }

    private Instructor(Guid id, DateTimeOffset createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Instructor Create(Guid id, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Instructor identifier is required.", nameof(id));
        return new Instructor(id, createdAt);
    }

    public void LinkUser(Guid userId, DateTimeOffset updatedAt)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User identifier is required.", nameof(userId));
        if (UserId is { } existingUserId && existingUserId != userId)
            throw new InvalidOperationException("Instructor is already linked to another User.");
        UserId = userId;
        UpdatedAt = updatedAt;
    }
}
