using AulaVirtual.Modules.Courses.Domain.Instructors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence.Configurations;

internal sealed class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.ToTable("instructors", "courses");
        builder.HasKey(instructor => instructor.Id).HasName("pk_instructors");
        builder.Property(instructor => instructor.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(instructor => instructor.UserId).HasColumnName("user_id");
        builder.Property(instructor => instructor.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(instructor => instructor.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(instructor => instructor.UserId).IsUnique()
            .HasFilter("user_id IS NOT NULL").HasDatabaseName("ux_instructors_user_id");
        // UserId is a logical cross-module reference; no EF navigation or PostgreSQL FK.
    }
}
