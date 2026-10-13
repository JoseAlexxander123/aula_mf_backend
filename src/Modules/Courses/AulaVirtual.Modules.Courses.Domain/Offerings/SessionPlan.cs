namespace AulaVirtual.Modules.Courses.Domain.Offerings;

public sealed record SessionPlan(Guid? Id, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? VirtualLink);
