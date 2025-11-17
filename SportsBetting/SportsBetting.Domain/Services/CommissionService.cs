using SportsBetting.Domain.Configuration;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Services;

/// <summary>
/// Implementation of commission calculation service
/// Uses configuration-based tiered rates and maker/taker discounts
/// </summary>
public class CommissionService : ICommissionService
{
    private readonly CommissionConfiguration _config;

    public CommissionService(CommissionConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Calculate commission for a winning bet
    /// </summary>
    public decimal CalculateCommission(User user, decimal grossWinnings, LiquidityRole liquidityRole)
    {
        if (grossWinnings <= 0)
            return 0;

        // Get the effective commission rate
        var rate = GetEffectiveRate(user, liquidityRole);

        // Calculate commission
        var commission = grossWinnings * rate;

        // Apply minimum commission
        if (commission > 0 && commission < _config.MinimumCommission)
            commission = _config.MinimumCommission;

        return Math.Round(commission, 2);
    }

    /// <summary>
    /// Get the effective commission rate for a user
    /// Combines tier-based rate with maker/taker discount
    /// </summary>
    public decimal GetEffectiveRate(User user, LiquidityRole liquidityRole)
    {
        return _config.GetEffectiveRate(user.CommissionTier, liquidityRole);
    }

    /// <summary>
    /// Calculate the appropriate commission tier based on 30-day volume
    /// </summary>
    public CommissionTier CalculateTier(decimal thirtyDayVolume)
    {
        return _config.CalculateTier(thirtyDayVolume);
    }

    /// <summary>
    /// Update a user's commission tier based on their current statistics
    /// Returns true if the tier was changed
    /// </summary>
    public bool UpdateUserTier(User user)
    {
        if (user.Statistics == null)
            return false;

        var currentTier = user.CommissionTier;
        var newTier = CalculateTier(user.Statistics.Volume30Day);

        if (newTier != currentTier)
        {
            user.UpdateCommissionTier(newTier);
            return true;
        }

        return false;
    }
}
