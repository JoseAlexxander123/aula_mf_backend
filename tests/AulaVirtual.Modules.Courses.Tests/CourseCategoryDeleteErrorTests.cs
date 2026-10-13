using AulaVirtual.Modules.Courses.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class CourseCategoryDeleteErrorTests
{
    [Theory]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "fk_courses_category_id", true)]
    [InlineData(PostgresErrorCodes.RestrictViolation, "fk_courses_category_id", true)]
    [InlineData(PostgresErrorCodes.ForeignKeyViolation, "another_constraint", false)]
    [InlineData(PostgresErrorCodes.RestrictViolation, "another_constraint", false)]
    [InlineData(PostgresErrorCodes.RestrictViolation, null, false)]
    [InlineData(PostgresErrorCodes.UniqueViolation, "fk_courses_category_id", false)]
    public void OnlyCourseCategoryReferenceErrorsBecomeConflicts(
        string sqlState, string? constraintName, bool expected)
    {
        var postgres = new PostgresException("category deletion failed", "ERROR", "ERROR", sqlState,
            constraintName: constraintName);
        var update = new DbUpdateException("category deletion failed", postgres);

        Assert.Equal(expected, CourseRepository.IsCategoryReferenceViolation(update));
    }

    [Fact]
    public void NonPostgresErrorsAreNotTranslated()
    {
        var update = new DbUpdateException("category deletion failed", new InvalidOperationException());

        Assert.False(CourseRepository.IsCategoryReferenceViolation(update));
    }
}
