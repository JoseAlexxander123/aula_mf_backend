using AulaVirtual.Modules.Courses.Domain.Courses;
using AulaVirtual.Modules.Courses.Domain.Instructors;
using AulaVirtual.Modules.Courses.Domain.Offerings;
using AulaVirtual.Modules.Courses.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AulaVirtual.Modules.Courses.Tests;

public sealed class PersistenceModelTests
{
    [Fact]
    public void CoursesModel_UsesOwnSchemaAndPublicationGuard()
    {
        using var context = new CoursesDbContext(
            new DbContextOptionsBuilder<CoursesDbContext>()
                .UseNpgsql("Host=localhost;Database=model_only;Username=model")
                .Options);

        var model = context.GetService<IDesignTimeModel>().Model;
        var course = model.FindEntityType(typeof(Course))!;
        var category = model.FindEntityType(typeof(CourseCategory))!;

        Assert.Equal("courses", course.GetSchema());
        Assert.Equal("courses", course.GetTableName());
        Assert.Equal("courses", category.GetSchema());
        Assert.Equal("course_categories", category.GetTableName());
        Assert.Equal(false, course.FindProperty(nameof(Course.IsPublished))!.GetDefaultValue());
        Assert.Contains(
            course.GetCheckConstraints(),
            constraint => constraint.Sql == "NOT is_published OR category_id IS NOT NULL");
        foreach (var property in new[] { nameof(Course.ShortDescription), nameof(Course.FullDescription),
            nameof(Course.Objectives), nameof(Course.Syllabus), nameof(Course.TargetAudience),
            nameof(Course.Prerequisites), nameof(Course.CoverObjectKey),
            nameof(Course.CoverContentType), nameof(Course.CoverSize),
            nameof(Course.CoverOriginalFileName) })
            Assert.True(course.FindProperty(property)!.IsNullable);
    }

    [Fact]
    public void OfferingModel_MapsOnlyCoursesOwnedRelationships()
    {
        using var context = new CoursesDbContext(
            new DbContextOptionsBuilder<CoursesDbContext>()
                .UseNpgsql("Host=localhost;Database=model_only;Username=model")
                .Options);

        var model = context.GetService<IDesignTimeModel>().Model;
        var offering = model.FindEntityType(typeof(CourseOffering))!;
        var session = model.FindEntityType(typeof(OfferingSession))!;

        Assert.Equal("courses", offering.GetSchema());
        Assert.Equal("course_offerings", offering.GetTableName());
        Assert.Equal("offering_sessions", session.GetTableName());
        Assert.Contains(offering.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Course) &&
            fk.Properties.Single().Name == nameof(CourseOffering.CourseId));
        Assert.Contains(session.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(CourseOffering) &&
            fk.Properties.Single().Name == nameof(OfferingSession.CourseOfferingId));
        Assert.Contains(session.GetCheckConstraints(), constraint => constraint.Sql == "starts_at < ends_at");
        var instructor = model.FindEntityType(typeof(Instructor))!;
        Assert.Equal("courses", instructor.GetSchema());
        Assert.Empty(instructor.GetForeignKeys());
        Assert.Contains(instructor.GetIndexes(), index => index.IsUnique &&
            index.Properties.Single().Name == nameof(Instructor.UserId));
        var assignment = model.FindEntityType(typeof(CourseOfferingInstructor))!;
        Assert.Equal(2, assignment.GetForeignKeys().Count());
        Assert.Contains(assignment.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(Instructor));
        Assert.Contains(assignment.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(CourseOffering));
        var promotion = model.FindEntityType(typeof(SpecificPromotion))!;
        Assert.Equal("course_offering_promotions", promotion.GetTableName());
        Assert.Single(promotion.GetForeignKeys());
        Assert.Contains(offering.GetCheckConstraints(), constraint =>
            constraint.Name == "ck_course_offerings_currency_code");
    }
}
