using AulaVirtual.Modules.Courses.Domain.Courses;
using AulaVirtual.Modules.Courses.Domain.Instructors;
using AulaVirtual.Modules.Courses.Domain.Offerings;
using Microsoft.EntityFrameworkCore;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence;

public sealed class CoursesDbContext(DbContextOptions<CoursesDbContext> options)
    : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseCategory> Categories => Set<CourseCategory>();
    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();
    public DbSet<OfferingSession> OfferingSessions => Set<OfferingSession>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<CourseOfferingInstructor> CourseOfferingInstructors => Set<CourseOfferingInstructor>();
    public DbSet<SpecificPromotion> SpecificPromotions => Set<SpecificPromotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("courses");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoursesDbContext).Assembly);
    }
}
