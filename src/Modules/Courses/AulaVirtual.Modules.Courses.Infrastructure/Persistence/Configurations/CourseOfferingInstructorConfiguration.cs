using AulaVirtual.Modules.Courses.Domain.Instructors;
using AulaVirtual.Modules.Courses.Domain.Offerings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence.Configurations;

internal sealed class CourseOfferingInstructorConfiguration : IEntityTypeConfiguration<CourseOfferingInstructor>
{
    public void Configure(EntityTypeBuilder<CourseOfferingInstructor> builder)
    {
        builder.ToTable("course_offering_instructors", "courses");
        builder.HasKey(assignment => new { assignment.CourseOfferingId, assignment.InstructorId })
            .HasName("pk_course_offering_instructors");
        builder.Property(assignment => assignment.CourseOfferingId).HasColumnName("course_offering_id");
        builder.Property(assignment => assignment.InstructorId).HasColumnName("instructor_id");
        builder.HasOne<Instructor>().WithMany().HasForeignKey(assignment => assignment.InstructorId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_course_offering_instructors_instructor_id");
        builder.HasIndex(assignment => assignment.InstructorId)
            .HasDatabaseName("ix_course_offering_instructors_instructor_id");
    }
}
