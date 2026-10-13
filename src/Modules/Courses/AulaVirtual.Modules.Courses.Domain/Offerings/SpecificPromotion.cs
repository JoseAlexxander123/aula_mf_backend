namespace AulaVirtual.Modules.Courses.Domain.Offerings;

public sealed class SpecificPromotion
{
    private SpecificPromotion() { }

    private SpecificPromotion(Guid courseOfferingId, SpecificPromotionType type, decimal value,
        DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        CourseOfferingId = courseOfferingId;
        Type = type;
        Value = value;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public Guid CourseOfferingId { get; private set; }
    public SpecificPromotionType Type { get; private set; }
    public decimal Value { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }

    internal static SpecificPromotion Create(Guid courseOfferingId, SpecificPromotionType type,
        decimal value, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (courseOfferingId == Guid.Empty) throw new ArgumentException("Edition identifier is required.");
        Validate(type, value, startsAt, endsAt);
        return new SpecificPromotion(courseOfferingId, type, value, startsAt, endsAt);
    }

    internal void Update(SpecificPromotionType type, decimal value, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        Validate(type, value, startsAt, endsAt);
        Type = type;
        Value = value;
        StartsAt = startsAt;
        EndsAt = endsAt;
    }

    public bool IsActive(DateTimeOffset now) => IsActive(now, StartsAt, EndsAt);

    public static bool IsActive(DateTimeOffset now, DateTimeOffset startsAt, DateTimeOffset endsAt) =>
        now >= startsAt && now < endsAt;

    public decimal CalculatePrice(decimal basePrice) => CalculatePrice(Type, Value, basePrice);

    public static decimal CalculatePrice(SpecificPromotionType type, decimal value, decimal basePrice)
    {
        var discount = type == SpecificPromotionType.Percentage
            ? basePrice * value / 100m
            : value;
        return Math.Max(0m, basePrice - discount);
    }

    private static void Validate(SpecificPromotionType type, decimal value,
        DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (value <= 0 || (type == SpecificPromotionType.Percentage && value > 100))
            throw new ArgumentOutOfRangeException(nameof(value));
        if (startsAt.Offset != TimeSpan.Zero || endsAt.Offset != TimeSpan.Zero || startsAt >= endsAt)
            throw new ArgumentException("Promotion requires an ordered UTC validity window.");
    }
}
