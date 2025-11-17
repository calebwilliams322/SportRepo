using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Services;

/// <summary>
/// Service for calculating commissions on bet winnings
/// Implements tiered commission structure and liquidity provider incentives
/// </summary>
public interface ICommissionService
{
    /// <summary>
    /// Calculate commission for a winning bet
    /// </summary>
    /// <param name="user">The winning user</param>
    /// <param name="grossWinnings">Gross winnings before commission</param>
    /// <param name="liquidityRole">Whether user was maker or taker</param>
    /// <returns>Commission amount to charge</returns>
    decimal CalculateCommission(User user, decimal grossWinnings, LiquidityRole liquidityRole);

    /// <summary>
    /// Get the effective commission rate for a user
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="liquidityRole">Whether user was maker or taker</param>
    /// <returns>Commission rate as decimal (e.g., 0.05 for 5%)</returns>
    decimal GetEffectiveRate(User user, LiquidityRole liquidityRole);

    /// <summary>
    /// Calculate the appropriate commission tier based on 30-day volume
    /// </summary>
    /// <param name="thirtyDayVolume">Total volume in last 30 days</param>
    /// <returns>The commission tier</returns>
    CommissionTier CalculateTier(decimal thirtyDayVolume);

    /// <summary>
    /// Update a user's commission tier based on their current statistics
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <returns>True if tier was changed</returns>
    bool UpdateUserTier(User user);
}
