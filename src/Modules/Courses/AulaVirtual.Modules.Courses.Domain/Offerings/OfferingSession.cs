namespace AulaVirtual.Modules.Courses.Domain.Offerings;

public sealed class OfferingSession
{
    private OfferingSession() { }

    private OfferingSession(Guid id, Guid courseOfferingId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? virtualLink)
    {
        Id = id;
        CourseOfferingId = courseOfferingId;
        StartsAt = startsAt;
        EndsAt = endsAt;
        VirtualLink = virtualLink;
    }

    public Guid Id { get; private set; }
    public Guid CourseOfferingId { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public string? VirtualLink { get; private set; }

    internal static OfferingSession Create(Guid id, Guid courseOfferingId, DateTimeOffset startsAt, DateTimeOffset endsAt, string? virtualLink)
    {
        if (id == Guid.Empty || courseOfferingId == Guid.Empty)
            throw new ArgumentException("Session and edition identifiers are required.");

        ValidateSchedule(startsAt, endsAt);
        return new OfferingSession(id, courseOfferingId, startsAt, endsAt, NormalizeLink(virtualLink));
    }

    internal void RescheduleDraft(DateTimeOffset startsAt, DateTimeOffset endsAt, string? virtualLink)
    {
        ValidateSchedule(startsAt, endsAt);
        StartsAt = startsAt;
        EndsAt = endsAt;
        VirtualLink = NormalizeLink(virtualLink);
    }

    internal void ChangeVirtualLink(string? virtualLink) => VirtualLink = NormalizeLink(virtualLink);

    private static void ValidateSchedule(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        if (startsAt.Offset != TimeSpan.Zero || endsAt.Offset != TimeSpan.Zero)
            throw new ArgumentException("Session timestamps must use UTC offset zero.");
        if (startsAt >= endsAt)
            throw new ArgumentException("Session start must precede its end.");
    }

    private static string? NormalizeLink(string? virtualLink)
    {
        var trimmed = virtualLink?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > 2048 || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("Virtual link must be an absolute HTTP(S) URL.", nameof(virtualLink));
        return trimmed;
    }
}
