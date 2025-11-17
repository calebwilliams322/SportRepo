namespace SportsBetting.Domain.Configuration;

/// <summary>
/// Feature flags for gradual rollout of hybrid betting system
/// </summary>
public class FeatureFlags
{
    /// <summary>
    /// Master switch for P2P exchange betting
    /// </summary>
    public bool EnableExchange { get; set; } = false;

    /// <summary>
    /// Show both sportsbook and exchange options to users
    /// </summary>
    public bool EnableHybridMode { get; set; } = false;

    /// <summary>
    /// Specific markets enabled for exchange (empty = all markets when EnableExchange is true)
    /// </summary>
    public List<Guid> ExchangeMarketsWhitelist { get; set; } = new();

    /// <summary>
    /// Enable consensus odds validation against external APIs
    /// </summary>
    public bool ConsensusOddsValidation { get; set; } = false;

    /// <summary>
    /// Automatically match bets on placement (if false, manual matching only)
    /// </summary>
    public bool AutoMatching { get; set; } = true;

    /// <summary>
    /// Allow bets to be partially matched
    /// </summary>
    public bool AllowPartialMatching { get; set; } = true;

    /// <summary>
    /// Default commission rate for exchange bets (e.g., 0.02 = 2%)
    /// </summary>
    public decimal DefaultCommissionRate { get; set; } = 0.02m;

    /// <summary>
    /// Allowed deviation from consensus odds (percentage, e.g., 20.0 = ±20%)
    /// ADJUSTED: User wants 20% tolerance with warnings, not restrictions
    /// </summary>
    public decimal OddsTolerancePercent { get; set; } = 20.0m;

    /// <summary>
    /// If true, block bets that exceed tolerance. If false, show warning but allow.
    /// ADJUSTED: User wants warnings, not blocking
    /// </summary>
    public bool BlockOutOfRangeOdds { get; set; } = false;
}
