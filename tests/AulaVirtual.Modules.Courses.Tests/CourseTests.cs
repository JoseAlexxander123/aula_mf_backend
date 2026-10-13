using AulaVirtual.Modules.Courses.Domain.Courses;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class CourseTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NewCourse_IsInternalByDefault()
    {
        var course = Course.Create(Guid.NewGuid(), "  Álgebra  ", null, Now);

        Assert.Equal("Álgebra", course.Title);
        Assert.False(course.IsPublished);
        Assert.Null(course.CategoryId);
    }

    [Fact]
    public void Course_RequiresCategoryBeforePublication()
    {
        var course = Course.Create(Guid.NewGuid(), "Álgebra", null, Now);

        Assert.Throws<InvalidOperationException>(() => course.Publish(Now));
        Assert.False(course.IsPublished);

        var categoryId = Guid.NewGuid();
        course.AssignCategory(categoryId, Now);
        course.Publish(Now);
        Assert.True(course.IsPublished);
        Assert.Equal(categoryId, course.CategoryId);

        course.Unpublish(Now);
        Assert.False(course.IsPublished);
    }
}
