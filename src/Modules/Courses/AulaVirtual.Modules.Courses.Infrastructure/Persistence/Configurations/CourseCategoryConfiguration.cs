using AulaVirtual.Modules.Courses.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence.Configurations;

internal sealed class CourseCategoryConfiguration : IEntityTypeConfiguration<CourseCategory>
{
    public void Configure(EntityTypeBuilder<CourseCategory> builder)
    {
        builder.ToTable("course_categories", "courses");
        builder.HasKey(category => category.Id).HasName("pk_course_categories");
        builder.Property(category => category.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(category => category.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(category => category.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(category => category.UpdatedAt).HasColumnName("updated_at");
    }
}
