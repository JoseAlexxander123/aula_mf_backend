using AulaVirtual.Modules.Courses.Domain.Courses;
using AulaVirtual.Modules.Courses.Domain.Offerings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence.Configurations;

internal sealed class CourseOfferingConfiguration : IEntityTypeConfiguration<CourseOffering>
{
    public void Configure(EntityTypeBuilder<CourseOffering> builder)
    {
        builder.ToTable("course_offerings", "courses", table =>
        {
            table.HasCheckConstraint("ck_course_offerings_status", "status IN ('draft', 'published')");
            table.HasCheckConstraint("ck_course_offerings_currency_code",
                "currency_code IS NULL OR currency_code ~ '^[A-Z]{3}$'");
        });
        builder.HasKey(offering => offering.Id).HasName("pk_course_offerings");
        builder.Property(offering => offering.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(offering => offering.CourseId).HasColumnName("course_id").IsRequired();
        builder.Property(offering => offering.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(offering => offering.Status).HasColumnName("status").HasMaxLength(16)
            .HasConversion(value => value == CourseOfferingStatus.Draft ? "draft" : "published",
                value => value == "draft" ? CourseOfferingStatus.Draft : CourseOfferingStatus.Published)
            .HasDefaultValue(CourseOfferingStatus.Draft).IsRequired();
        builder.Property(offering => offering.MaximumCapacity).HasColumnName("maximum_capacity").IsRequired();
        builder.Property(offering => offering.BasePrice).HasColumnName("base_price").HasColumnType("numeric").IsRequired();
        builder.Property(offering => offering.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
        builder.Property(offering => offering.PlannedSessionCount).HasColumnName("planned_session_count").IsRequired();
        builder.Property(offering => offering.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(offering => offering.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Course>().WithMany().HasForeignKey(offering => offering.CourseId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_course_offerings_course_id");
        builder.HasIndex(offering => offering.CourseId).HasDatabaseName("ix_course_offerings_course_id");
        builder.HasMany(offering => offering.Sessions).WithOne()
            .HasForeignKey(session => session.CourseOfferingId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .HasConstraintName("fk_offering_sessions_course_offering_id");
        builder.Navigation(offering => offering.Sessions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(offering => offering.InstructorAssignments).WithOne()
            .HasForeignKey(assignment => assignment.CourseOfferingId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .HasConstraintName("fk_course_offering_instructors_offering_id");
        builder.Navigation(offering => offering.InstructorAssignments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne(offering => offering.SpecificPromotion).WithOne()
            .HasForeignKey<SpecificPromotion>(promotion => promotion.CourseOfferingId)
            .OnDelete(DeleteBehavior.ClientCascade)
            .HasConstraintName("fk_course_offering_promotions_offering_id");
    }
}
