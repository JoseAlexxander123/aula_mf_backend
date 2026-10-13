using AulaVirtual.Modules.Courses.Domain.Offerings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence.Configurations;

internal sealed class OfferingSessionConfiguration : IEntityTypeConfiguration<OfferingSession>
{
    public void Configure(EntityTypeBuilder<OfferingSession> builder)
    {
        builder.ToTable("offering_sessions", "courses", table =>
            table.HasCheckConstraint("ck_offering_sessions_time", "starts_at < ends_at"));
        builder.HasKey(session => session.Id).HasName("pk_offering_sessions");
        builder.Property(session => session.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(session => session.CourseOfferingId).HasColumnName("course_offering_id").IsRequired();
        builder.Property(session => session.StartsAt).HasColumnName("starts_at").IsRequired();
        builder.Property(session => session.EndsAt).HasColumnName("ends_at").IsRequired();
        builder.Property(session => session.VirtualLink).HasColumnName("virtual_link").HasMaxLength(2048);
        builder.HasIndex(session => session.CourseOfferingId)
            .HasDatabaseName("ix_offering_sessions_course_offering_id");
    }
}
