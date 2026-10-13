using AulaVirtual.Modules.Courses.Domain.Offerings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AulaVirtual.Modules.Courses.Infrastructure.Persistence.Configurations;

internal sealed class SpecificPromotionConfiguration : IEntityTypeConfiguration<SpecificPromotion>
{
    public void Configure(EntityTypeBuilder<SpecificPromotion> builder)
    {
        builder.ToTable("course_offering_promotions", "courses", table =>
        {
            table.HasCheckConstraint("ck_course_offering_promotions_window", "starts_at < ends_at");
            table.HasCheckConstraint("ck_course_offering_promotions_value",
                "value > 0 AND (type <> 'percentage' OR value <= 100)");
            table.HasCheckConstraint("ck_course_offering_promotions_type",
                "type IN ('percentage', 'fixed_amount')");
        });
        builder.HasKey(promotion => promotion.CourseOfferingId)
            .HasName("pk_course_offering_promotions");
        builder.Property(promotion => promotion.CourseOfferingId)
            .HasColumnName("course_offering_id").ValueGeneratedNever();
        builder.Property(promotion => promotion.Type).HasColumnName("type").HasMaxLength(16)
            .HasConversion(value => value == SpecificPromotionType.Percentage ? "percentage" : "fixed_amount",
                value => value == "percentage" ? SpecificPromotionType.Percentage : SpecificPromotionType.FixedAmount)
            .IsRequired();
        builder.Property(promotion => promotion.Value).HasColumnName("value").HasColumnType("numeric").IsRequired();
        builder.Property(promotion => promotion.StartsAt).HasColumnName("starts_at").IsRequired();
        builder.Property(promotion => promotion.EndsAt).HasColumnName("ends_at").IsRequired();
    }
}
