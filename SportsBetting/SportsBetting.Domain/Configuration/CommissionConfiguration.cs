using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Configuration;

/// <summary>
/// Configuration for commission rates and liquidity provider incentives
/// Can be loaded from appsettings.json for easy adjustment
/// </summary>
public class CommissionConfiguration
{
    /// <summary>
    /// Commission rates by tier (as decimal, e.g., 0.015 = 1.5%)
    /// </summary>
    public Dictionary<CommissionTier, decimal> TierRates { get; set; } = new()
    {
        { CommissionTier.Standard, 0.015m },   // 1.5%
        { CommissionTier.Bronze, 0.0125m },    // 1.25%
        { CommissionTier.Silver, 0.01m },      // 1%
        { CommissionTier.Gold, 0.0075m },      // 0.75%
        { CommissionTier.Platinum, 0.005m }    // 0.5%
    };

    /// <summary>
    /// Volume thresholds for each tier (30-day USD volume)
    /// </summary>
    public Dictionary<CommissionTier, decimal> TierThresholds { get; set; } = new()
    {
        { CommissionTier.Standard, 0m },
        { CommissionTier.Bronze, 10_000m },
        { CommissionTier.Silver, 50_000m },
        { CommissionTier.Gold, 200_000m },
        { CommissionTier.Platinum, 1_000_000m }
    };

    /// <summary>
    /// Discount for liquidity providers (makers)
    /// Applied as percentage off the base commission rate
    /// Default: 0.20 = 20% discount
    /// Example: 5% base rate - 20% discount = 4% effective rate
    /// </summary>
    public decimal MakerDiscount { get; set; } = 0.20m;

    /// <summary>
    /// Number of days to calculate rolling volume for tier assignment
    /// Default: 30 days
    /// </summary>
    public int VolumeCalculationDays { get; set; } = 30;

    /// <summary>
    /// Minimum commission amount (to prevent rounding to zero on small bets)
    /// Default: $0.01
    /// </summary>
    public decimal MinimumCommission { get; set; } = 0.01m;

    /// <summary>
    /// Whether to charge commission only on net winnings (true) or gross winnings (false)
    /// Betfair uses net winnings
    /// Default: true
    /// </summary>
    public bool ChargeOnNetWinningsOnly { get; set; } = true;

    /// <summary>
    /// Get the commission rate for a given tier and liquidity role
    /// </summary>
    public decimal GetEffectiveRate(CommissionTier tier, LiquidityRole role)
    {
        var baseRate = TierRates[tier];

        if (role == LiquidityRole.Maker)
        {
            // Apply maker discount
            return baseRate * (1 - MakerDiscount);
        }

        return baseRate;
    }

    /// <summary>
    /// Calculate the commission tier based on 30-day volume
    /// </summary>
    public CommissionTier CalculateTier(decimal thirtyDayVolume)
    {
        if (thirtyDayVolume >= TierThresholds[CommissionTier.Platinum])
            return CommissionTier.Platinum;
        if (thirtyDayVolume >= TierThresholds[CommissionTier.Gold])
            return CommissionTier.Gold;
        if (thirtyDayVolume >= TierThresholds[CommissionTier.Silver])
            return CommissionTier.Silver;
        if (thirtyDayVolume >= TierThresholds[CommissionTier.Bronze])
            return CommissionTier.Bronze;

        return CommissionTier.Standard;
    }
}
