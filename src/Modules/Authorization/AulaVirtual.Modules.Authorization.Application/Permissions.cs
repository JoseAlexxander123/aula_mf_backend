namespace AulaVirtual.Modules.Authorization.Application;

// AulaVirtual catalog data, separate from the generic Authorization domain.
public static class Permissions
{
    public const string CoursesManage = "courses.manage";
    public const string CourseOfferingsManage = "course_offerings.manage";
    public const string InstructorsManage = "instructors.manage";
    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    { CoursesManage, CourseOfferingsManage, InstructorsManage };
}

public static class Roles
{
    public const string Administrator = "administrator";
    public const string Student = "student";
    public const string Instructor = "instructor";
}
