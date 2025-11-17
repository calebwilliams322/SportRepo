using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;

namespace SportsBetting.API.Services;

/// <summary>
/// Tracks house revenue from both Sportsbook and Exchange operations
/// Creates hourly revenue records for fine-grained tracking
/// </summary>
public class RevenueService : IRevenueService
{
    private readonly SportsBettingDbContext _context;

    public RevenueService(SportsBettingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Record revenue from a sportsbook bet settlement
    /// </summary>
    public void RecordSportsbookSettlement(Bet bet, DateTime? settlementTime = null)
    {
        if (bet.BetMode != BetMode.Sportsbook)
            throw new InvalidOperationException("Bet must be in Sportsbook mode");

        if (bet.Status != BetStatus.Won && bet.Status != BetStatus.Lost)
            throw new InvalidOperationException("Bet must be Won or Lost to record revenue");

        var timestamp = settlementTime ?? DateTime.UtcNow;
        var revenue = GetOrCreateRevenueForPeriod(timestamp);

        if (bet.Status == BetStatus.Lost)
        {
            // House wins - customer lost their stake
            revenue.RecordSportsbookLoss(bet.Stake.Amount);
        }
        else if (bet.Status == BetStatus.Won)
        {
            // House pays out - customer won
            var payout = bet.ActualPayout?.Amount ?? 0m;
            revenue.RecordSportsbookWin(bet.Stake.Amount, payout);
        }

        // SaveChanges will be called by the calling code
    }

    /// <summary>
    /// Record commission revenue from an exchange match settlement
    /// </summary>
    public void RecordExchangeSettlement(
        BetMatch match,
        decimal commission,
        decimal winnerPayout,
        DateTime? settlementTime = null)
    {
        if (!match.IsSettled)
            throw new InvalidOperationException("Match must be settled");

        var timestamp = settlementTime ?? DateTime.UtcNow;
        var revenue = GetOrCreateRevenueForPeriod(timestamp);

        revenue.RecordExchangeCommission(
            commission: commission,
            matchedStake: match.MatchedStake,
            winnerPayout: winnerPayout
        );

        // SaveChanges will be called by the calling code
    }

    /// <summary>
    /// Get or create the current hour's revenue record
    /// </summary>
    public HouseRevenue GetOrCreateCurrentHourRevenue()
    {
        return GetOrCreateRevenueForPeriod(DateTime.UtcNow, "Hourly");
    }

    /// <summary>
    /// Get or create a revenue record for a specific period
    /// </summary>
    public HouseRevenue GetOrCreateRevenueForPeriod(DateTime timestamp, string periodType = "Hourly")
    {
        DateTime periodStart, periodEnd;

        switch (periodType)
        {
            case "Hourly":
                periodStart = new DateTime(
                    timestamp.Year,
                    timestamp.Month,
                    timestamp.Day,
                    timestamp.Hour,
                    0,
                    0,
                    DateTimeKind.Utc
                );
                periodEnd = periodStart.AddHours(1);
                break;

            case "Daily":
                periodStart = timestamp.Date;
                periodEnd = periodStart.AddDays(1);
                break;

            case "Monthly":
                periodStart = new DateTime(timestamp.Year, timestamp.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                periodEnd = periodStart.AddMonths(1);
                break;

            default:
                throw new ArgumentException($"Invalid period type: {periodType}");
        }

        // Try to find existing revenue record for this period
        var existing = _context.HouseRevenue
            .FirstOrDefault(r =>
                r.PeriodStart == periodStart &&
                r.PeriodEnd == periodEnd &&
                r.PeriodType == periodType
            );

        if (existing != null)
            return existing;

        // Create new revenue record
        var revenue = new HouseRevenue(periodStart, periodEnd, periodType);
        _context.HouseRevenue.Add(revenue);

        return revenue;
    }
}
