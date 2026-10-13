using AulaVirtual.Modules.Courses.Application.Offerings;
using AulaVirtual.Modules.Courses.Domain.Offerings;

namespace AulaVirtual.Modules.Courses.Api.Contracts;

public sealed record OfferingSessionRequest(Guid? Id, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, string? VirtualLink)
{
    public SessionPlan ToPlan() => new(Id, StartsAt, EndsAt, VirtualLink);
}

public sealed record SaveCourseOfferingRequest(string Name, int MaximumCapacity, decimal BasePrice,
    string CurrencyCode,
    int PlannedSessionCount, IReadOnlyList<OfferingSessionRequest> Sessions);

public sealed record UpdateVirtualLinkRequest(string? VirtualLink);

public sealed record OfferingSessionResponse(Guid Id, DateTimeOffset StartsAt,
    DateTimeOffset EndsAt, string? VirtualLink);

public sealed record CourseOfferingResponse(Guid Id, Guid CourseId, string Name, string Status,
    int MaximumCapacity, decimal BasePrice, string? CurrencyCode, int PlannedSessionCount,
    IReadOnlyList<Guid> InstructorIds, SpecificPromotionResponse? SpecificPromotion,
    IReadOnlyList<OfferingSessionResponse> Sessions)
{
    public static CourseOfferingResponse From(CourseOfferingDetails details) => new(
        details.Id, details.CourseId, details.Name,
        details.Status == CourseOfferingStatus.Draft ? "draft" : "published",
        details.MaximumCapacity, details.BasePrice, details.CurrencyCode, details.PlannedSessionCount,
        details.InstructorIds,
        details.SpecificPromotion is { } promotion ? new SpecificPromotionResponse(
            promotion.Type.ToString(), promotion.Value, promotion.StartsAt, promotion.EndsAt) : null,
        details.Sessions.Select(session => new OfferingSessionResponse(session.Id,
            session.StartsAt, session.EndsAt, session.VirtualLink)).ToArray());
}

public sealed record SpecificPromotionResponse(string Type, decimal Value,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt);
