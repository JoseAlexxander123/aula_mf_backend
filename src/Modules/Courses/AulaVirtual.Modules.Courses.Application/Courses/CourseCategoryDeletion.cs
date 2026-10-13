namespace AulaVirtual.Modules.Courses.Application.Courses;

public enum CourseCategoryDeletion
{
    Deleted,
    NotFound,
    InUse
}

public sealed class CourseCategoryInUseException : Exception
{
    public CourseCategoryInUseException()
        : base("The category is assigned to a course.")
    {
    }
}
