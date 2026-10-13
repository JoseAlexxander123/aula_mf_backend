using AulaVirtual.Modules.Courses.Domain.Offerings;

namespace AulaVirtual.Modules.Courses.Api.Contracts;

public sealed record AssignInstructorsRequest(IReadOnlyList<Guid> InstructorIds);
public sealed record UpdateCurrencyRequest(string CurrencyCode);
public sealed record SetSpecificPromotionRequest(string Type, decimal Value,
    DateTimeOffset StartsAt, DateTimeOffset EndsAt);

public sealed record PricePreviewResponse(decimal BasePrice, string CurrencyCode,
    decimal? SpecificOrBasePrice, string Source, bool GlobalCampaignEvaluated)
{
    public static PricePreviewResponse From(PriceQuote quote) => new(
        quote.BasePrice, quote.CurrencyCode, quote.EffectivePrice,
        quote.Source.ToString(), false);
}
