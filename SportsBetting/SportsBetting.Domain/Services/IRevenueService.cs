using SportsBetting.Domain.Entities;

namespace SportsBetting.Domain.Services;

/// <summary>
/// Service for tracking house revenue from both Sportsbook and Exchange operations
/// </summary>
public interface IRevenueService
{
    /// <summary>
    /// Record revenue from a sportsbook bet settlement
    /// </summary>
    void RecordSportsbookSettlement(Bet bet, DateTime? settlementTime = null);

    /// <summary>
    /// Record commission revenue from an exchange match settlement
    /// </summary>
    void RecordExchangeSettlement(
        BetMatch match,
        decimal commission,
        decimal winnerPayout,
        DateTime? settlementTime = null);

    /// <summary>
    /// Get or create the current hour's revenue record
    /// </summary>
    HouseRevenue GetOrCreateCurrentHourRevenue();

    /// <summary>
    /// Get or create a revenue record for a specific period
    /// </summary>
    HouseRevenue GetOrCreateRevenueForPeriod(DateTime timestamp, string periodType = "Hourly");
}
