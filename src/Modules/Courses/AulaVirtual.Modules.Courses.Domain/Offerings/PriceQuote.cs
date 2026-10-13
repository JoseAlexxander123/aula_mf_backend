namespace AulaVirtual.Modules.Courses.Domain.Offerings;

public enum PriceSource
{
    Base,
    SpecificPromotion,
    GlobalCampaignPending
}

public sealed record PriceQuote(decimal BasePrice, string CurrencyCode,
    decimal? EffectivePrice, PriceSource Source);
