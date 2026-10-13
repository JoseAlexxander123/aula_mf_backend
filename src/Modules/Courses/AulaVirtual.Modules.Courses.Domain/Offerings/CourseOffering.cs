namespace AulaVirtual.Modules.Courses.Domain.Offerings;

public sealed class CourseOffering
{
    private readonly List<OfferingSession> _sessions = [];
    private readonly List<CourseOfferingInstructor> _instructorAssignments = [];

    private CourseOffering() { }

    private CourseOffering(Guid id, Guid courseId, string name, int maximumCapacity, decimal basePrice,
        int plannedSessionCount, string currencyCode, DateTimeOffset createdAt)
    {
        Id = id;
        CourseId = courseId;
        Name = name;
        MaximumCapacity = maximumCapacity;
        BasePrice = basePrice;
        CurrencyCode = CurrencyCodeRules.Validate(currencyCode);
        PlannedSessionCount = plannedSessionCount;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid CourseId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public CourseOfferingStatus Status { get; private set; }
    public int MaximumCapacity { get; private set; }
    public decimal BasePrice { get; private set; }
    // Nullable only while pre-NODE-014 editions await a verified currency backfill.
    public string? CurrencyCode { get; private set; }
    public SpecificPromotion? SpecificPromotion { get; private set; }
    public IReadOnlyList<CourseOfferingInstructor> InstructorAssignments => _instructorAssignments;
    public IReadOnlyList<Guid> InstructorIds => _instructorAssignments.Select(item => item.InstructorId).ToArray();
    public int PlannedSessionCount { get; private set; }
    public IReadOnlyList<OfferingSession> Sessions => _sessions;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static CourseOffering Create(Guid id, Guid courseId, string name, int maximumCapacity,
        decimal basePrice, int plannedSessionCount, string currencyCode,
        IEnumerable<SessionPlan> sessions, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty || courseId == Guid.Empty)
            throw new ArgumentException("Edition and Course identifiers are required.");
        ValidateDetails(name, maximumCapacity, basePrice, plannedSessionCount);
        var offering = new CourseOffering(id, courseId, name.Trim(), maximumCapacity, basePrice,
            plannedSessionCount, currencyCode, createdAt);
        offering.ReplaceDraftSessions(sessions);
        return offering;
    }

    public void Update(string name, int maximumCapacity, decimal basePrice, int plannedSessionCount,
        string currencyCode,
        IEnumerable<SessionPlan> sessions, DateTimeOffset updatedAt)
    {
        EnsureDraft();
        ValidateDetails(name, maximumCapacity, basePrice, plannedSessionCount);
        ValidateCurrencyChange(currencyCode);
        var plans = ValidatePlans(sessions, plannedSessionCount);
        var existing = _sessions.ToDictionary(session => session.Id);
        var validated = plans.Select(plan => OfferingSession.Create(
            plan.Id ?? Guid.NewGuid(), Id, plan.StartsAt, plan.EndsAt, plan.VirtualLink)).ToList();
        foreach (var plan in plans.Where(plan => plan.Id.HasValue))
        {
            if (!existing.ContainsKey(plan.Id!.Value))
                throw new ArgumentException("Session does not belong to this edition.", nameof(sessions));
        }
        var next = new List<OfferingSession>(plans.Count);
        for (var index = 0; index < plans.Count; index++)
        {
            var plan = plans[index];
            if (plan.Id is { } sessionId)
            {
                var session = existing[sessionId];
                session.RescheduleDraft(plan.StartsAt, plan.EndsAt, plan.VirtualLink);
                next.Add(session);
            }
            else
            {
                next.Add(validated[index]);
            }
        }

        Name = name.Trim();
        MaximumCapacity = maximumCapacity;
        BasePrice = basePrice;
        CurrencyCode = currencyCode;
        PlannedSessionCount = plannedSessionCount;
        _sessions.Clear();
        _sessions.AddRange(next);
        UpdatedAt = updatedAt;
    }

    public void Publish(DateTimeOffset updatedAt)
    {
        if (CurrencyCode is null) throw new InvalidOperationException("Edition requires a currency before publication.");
        if (_instructorAssignments.Count == 0)
            throw new InvalidOperationException("Edition requires at least one Instructor before publication.");
        if (_sessions.Count != PlannedSessionCount || PlannedSessionCount <= 0)
            throw new InvalidOperationException("Edition requires its complete session calendar before publication.");
        Status = CourseOfferingStatus.Published;
        UpdatedAt = updatedAt;
    }

    public void SetCurrencyCode(string currencyCode, DateTimeOffset updatedAt)
    {
        ValidateCurrencyChange(currencyCode);
        CurrencyCode = currencyCode;
        UpdatedAt = updatedAt;
    }

    public void AssignInstructors(IEnumerable<Guid> instructorIds, DateTimeOffset updatedAt)
    {
        EnsureDraft();
        ArgumentNullException.ThrowIfNull(instructorIds);
        var ids = instructorIds.ToArray();
        if (ids.Length == 0 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("Edition requires distinct Instructor identifiers.", nameof(instructorIds));
        var existing = _instructorAssignments.ToDictionary(item => item.InstructorId);
        var next = ids.Select(id => existing.TryGetValue(id, out var assignment)
            ? assignment : CourseOfferingInstructor.Create(Id, id)).ToArray();
        _instructorAssignments.Clear();
        _instructorAssignments.AddRange(next);
        UpdatedAt = updatedAt;
    }

    public void SetSpecificPromotion(SpecificPromotionType type, decimal value,
        DateTimeOffset startsAt, DateTimeOffset endsAt, DateTimeOffset updatedAt)
    {
        if (CurrencyCode is null) throw new InvalidOperationException("Edition requires a currency before promotion.");
        if (SpecificPromotion is null)
            SpecificPromotion = SpecificPromotion.Create(Id, type, value, startsAt, endsAt);
        else
            SpecificPromotion.Update(type, value, startsAt, endsAt);
        UpdatedAt = updatedAt;
    }

    public void RemoveSpecificPromotion(DateTimeOffset updatedAt)
    {
        SpecificPromotion = null;
        UpdatedAt = updatedAt;
    }

    public PriceQuote ResolvePrice(DateTimeOffset now, bool hasActiveGlobalCampaign = false)
    {
        if (CurrencyCode is null) throw new InvalidOperationException("Edition currency has not been resolved.");
        if (SpecificPromotion is { } promotion && promotion.IsActive(now))
            return new PriceQuote(BasePrice, CurrencyCode, promotion.CalculatePrice(BasePrice),
                PriceSource.SpecificPromotion);
        if (hasActiveGlobalCampaign)
            return new PriceQuote(BasePrice, CurrencyCode, null, PriceSource.GlobalCampaignPending);
        return new PriceQuote(BasePrice, CurrencyCode, BasePrice, PriceSource.Base);
    }

    private void ValidateCurrencyChange(string currencyCode)
    {
        CurrencyCodeRules.Validate(currencyCode);
        if (CurrencyCode is not null && CurrencyCode != currencyCode &&
            SpecificPromotion?.Type == SpecificPromotionType.FixedAmount)
            throw new InvalidOperationException("Remove the fixed-amount promotion before changing currency.");
    }

    public void UpdateSessionVirtualLink(Guid sessionId, string? virtualLink, DateTimeOffset updatedAt)
    {
        var session = _sessions.SingleOrDefault(item => item.Id == sessionId)
            ?? throw new ArgumentException("Session does not belong to this edition.", nameof(sessionId));
        session.ChangeVirtualLink(virtualLink);
        UpdatedAt = updatedAt;
    }

    private void ReplaceDraftSessions(IEnumerable<SessionPlan> sessions)
    {
        var plans = ValidatePlans(sessions, PlannedSessionCount);
        foreach (var plan in plans)
        {
            if (plan.Id is not null)
                throw new ArgumentException("New sessions cannot have an existing identifier.", nameof(sessions));
            _sessions.Add(OfferingSession.Create(Guid.NewGuid(), Id, plan.StartsAt, plan.EndsAt, plan.VirtualLink));
        }
    }

    private static List<SessionPlan> ValidatePlans(IEnumerable<SessionPlan> sessions, int plannedSessionCount)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        var plans = sessions.ToList();
        if (plans.Count != plannedSessionCount || plans.Where(plan => plan.Id.HasValue)
            .Select(plan => plan.Id!.Value).Distinct().Count() != plans.Count(plan => plan.Id.HasValue))
            throw new ArgumentException("Sessions must match the planned count and have distinct identifiers.", nameof(sessions));
        return plans;
    }

    private static void ValidateDetails(string name, int maximumCapacity, decimal basePrice, int plannedSessionCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > 200) throw new ArgumentOutOfRangeException(nameof(name));
        if (maximumCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCapacity));
        if (basePrice < 0) throw new ArgumentOutOfRangeException(nameof(basePrice));
        if (plannedSessionCount <= 0) throw new ArgumentOutOfRangeException(nameof(plannedSessionCount));
    }

    private void EnsureDraft()
    {
        if (Status != CourseOfferingStatus.Draft)
            throw new InvalidOperationException("Published edition calendar is not editable through draft editing.");
    }
}
