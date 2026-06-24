namespace GeorgiaERP.Domain.CRM;

/// <summary>
/// Maps a customer's lifetime spend to a loyalty tier. Spend-based tiering is
/// the common retail model; thresholds are in GEL. Centralised here so the tier
/// rule has a single source of truth for both recalculation and reporting.
/// </summary>
public static class LoyaltyTierPolicy
{
    public const string Bronze = "Bronze";
    public const string Silver = "Silver";
    public const string Gold = "Gold";

    public const decimal SilverThreshold = 1000m;
    public const decimal GoldThreshold = 5000m;

    public static string ForSpend(decimal totalPurchases) =>
        totalPurchases >= GoldThreshold ? Gold
        : totalPurchases >= SilverThreshold ? Silver
        : Bronze;
}
