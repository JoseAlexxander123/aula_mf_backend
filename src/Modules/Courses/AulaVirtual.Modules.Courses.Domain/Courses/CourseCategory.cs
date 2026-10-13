namespace AulaVirtual.Modules.Courses.Domain.Courses;

public sealed class CourseCategory
{
    private CourseCategory()
    {
    }

    private CourseCategory(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static CourseCategory Create(Guid id, string name, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A category requires an identifier.", nameof(id));
        }

        return new CourseCategory(id, NormalizeName(name), createdAt);
    }

    public void Rename(string name, DateTimeOffset updatedAt)
    {
        Name = NormalizeName(name);
        UpdatedAt = updatedAt;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmedName = name.Trim();
        if (trimmedName.Length > 150)
        {
            throw new ArgumentOutOfRangeException(nameof(name));
        }

        return trimmedName;
    }
}
