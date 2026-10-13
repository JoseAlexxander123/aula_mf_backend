using AulaVirtual.Modules.Courses.Domain.Offerings;

namespace AulaVirtual.Modules.Courses.Application.Offerings;

public sealed record CreateCourseOfferingCommand(Guid CourseId, string Name, int MaximumCapacity,
    decimal BasePrice, string CurrencyCode, int PlannedSessionCount, IReadOnlyList<SessionPlan> Sessions);

public sealed record UpdateCourseOfferingCommand(Guid CourseId, Guid OfferingId, string Name,
    int MaximumCapacity, decimal BasePrice, string CurrencyCode,
    int PlannedSessionCount, IReadOnlyList<SessionPlan> Sessions);

public sealed record SpecificPromotionDetails(SpecificPromotionType Type, decimal Value,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt);

public sealed record OfferingSessionDetails(Guid Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt,
    string? VirtualLink);

public sealed record CourseOfferingDetails(Guid Id, Guid CourseId, string Name, CourseOfferingStatus Status,
    int MaximumCapacity, decimal BasePrice, string? CurrencyCode, int PlannedSessionCount,
    IReadOnlyList<Guid> InstructorIds, SpecificPromotionDetails? SpecificPromotion,
    IReadOnlyList<OfferingSessionDetails> Sessions)
{
    public static CourseOfferingDetails From(CourseOffering offering) => new(
        offering.Id, offering.CourseId, offering.Name, offering.Status, offering.MaximumCapacity,
        offering.BasePrice, offering.CurrencyCode, offering.PlannedSessionCount,
        offering.InstructorIds,
        offering.SpecificPromotion is { } promotion ? new SpecificPromotionDetails(
            promotion.Type, promotion.Value, promotion.StartsAt, promotion.EndsAt) : null,
        offering.Sessions.OrderBy(session => session.StartsAt)
            .Select(session => new OfferingSessionDetails(session.Id, session.StartsAt, session.EndsAt,
                session.VirtualLink)).ToArray());
}
