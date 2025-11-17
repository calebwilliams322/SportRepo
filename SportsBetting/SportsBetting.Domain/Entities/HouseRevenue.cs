namespace SportsBetting.Domain.Entities;

/// <summary>
/// Tracks house revenue from both Sportsbook (book) and Exchange (P2P) operations
/// Records are created per day/hour for aggregation and reporting
/// </summary>
public class HouseRevenue
{
    public Guid Id { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public string PeriodType { get; private set; } // "Hourly", "Daily", "Monthly"

    // === SPORTSBOOK (BOOK) REVENUE ===
    // Traditional house betting where house takes the other side

    /// <summary>
    /// Total stakes from losing sportsbook bets (house revenue)
    /// </summary>
    public decimal SportsbookGrossRevenue { get; private set; }

    /// <summary>
    /// Total payouts to winning sportsbook bets (house expense)
    /// </summary>
    public decimal SportsbookPayouts { get; private set; }

    /// <summary>
    /// Net sportsbook profit (Gross Revenue - Payouts)
    /// </summary>
    public decimal SportsbookNetRevenue { get; private set; }

    /// <summary>
    /// Number of sportsbook bets settled this period
    /// </summary>
    public int SportsbookBetsSettled { get; private set; }

    /// <summary>
    /// Total volume of sportsbook bets settled
    /// </summary>
    public decimal SportsbookVolume { get; private set; }

    // === EXCHANGE (P2P) REVENUE ===
    // Peer-to-peer betting where house earns commission only

    /// <summary>
    /// Total commission earned from exchange bets (house revenue)
    /// </summary>
    public decimal ExchangeCommissionRevenue { get; private set; }

    /// <summary>
    /// Number of exchange matches settled this period
    /// </summary>
    public int ExchangeMatchesSettled { get; private set; }

    /// <summary>
    /// Total volume of exchange bets matched
    /// </summary>
    public decimal ExchangeVolume { get; private set; }

    /// <summary>
    /// Total payouts to exchange winners (customer-to-customer, not house expense)
    /// Tracked for analytics only - doesn't affect house profit
    /// </summary>
    public decimal ExchangeCustomerPayouts { get; private set; }

    // === COMBINED METRICS ===

    /// <summary>
    /// Total house revenue (Sportsbook Net + Exchange Commission)
    /// </summary>
    public decimal TotalRevenue { get; private set; }

    /// <summary>
    /// Total volume across both modes
    /// </summary>
    public decimal TotalVolume { get; private set; }

    /// <summary>
    /// Effective margin percentage (Total Revenue / Total Volume * 100)
    /// </summary>
    public decimal EffectiveMargin { get; private set; }

    // === METADATA ===
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdated { get; private set; }

    // Private constructor for EF Core
    private HouseRevenue()
    {
        PeriodType = "";
    }

    public HouseRevenue(DateTime periodStart, DateTime periodEnd, string periodType)
    {
        Id = Guid.NewGuid();
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        PeriodType = periodType;
        CreatedAt = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Record a sportsbook bet loss (house wins)
    /// </summary>
    public void RecordSportsbookLoss(decimal stake)
    {
        SportsbookGrossRevenue += stake;
        SportsbookVolume += stake;
        SportsbookBetsSettled++;

        RecalculateTotals();
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Record a sportsbook bet win (house pays out)
    /// </summary>
    public void RecordSportsbookWin(decimal stake, decimal payout)
    {
        SportsbookPayouts += payout;
        SportsbookVolume += stake;
        SportsbookBetsSettled++;

        RecalculateTotals();
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Record exchange commission from a settled match
    /// </summary>
    public void RecordExchangeCommission(decimal commission, decimal matchedStake, decimal winnerPayout)
    {
        ExchangeCommissionRevenue += commission;
        ExchangeVolume += matchedStake;
        ExchangeCustomerPayouts += winnerPayout;
        ExchangeMatchesSettled++;

        RecalculateTotals();
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculate all derived totals
    /// </summary>
    private void RecalculateTotals()
    {
        // Sportsbook net = revenue from losers - payouts to winners
        SportsbookNetRevenue = SportsbookGrossRevenue - SportsbookPayouts;

        // Total house revenue = sportsbook profit + exchange commission
        TotalRevenue = SportsbookNetRevenue + ExchangeCommissionRevenue;

        // Total volume
        TotalVolume = SportsbookVolume + ExchangeVolume;

        // Effective margin
        EffectiveMargin = TotalVolume > 0
            ? (TotalRevenue / TotalVolume) * 100
            : 0;
    }

    /// <summary>
    /// Get a summary of this revenue period
    /// </summary>
    public RevenueSummary GetSummary()
    {
        return new RevenueSummary
        {
            PeriodStart = PeriodStart,
            PeriodEnd = PeriodEnd,
            PeriodType = PeriodType,

            SportsbookRevenue = SportsbookNetRevenue,
            SportsbookVolume = SportsbookVolume,
            SportsbookBetsCount = SportsbookBetsSettled,
            SportsbookHoldPercentage = SportsbookVolume > 0
                ? (SportsbookNetRevenue / SportsbookVolume) * 100
                : 0,

            ExchangeRevenue = ExchangeCommissionRevenue,
            ExchangeVolume = ExchangeVolume,
            ExchangeMatchesCount = ExchangeMatchesSettled,
            ExchangeEffectiveRate = ExchangeVolume > 0
                ? (ExchangeCommissionRevenue / ExchangeVolume) * 100
                : 0,

            TotalRevenue = TotalRevenue,
            TotalVolume = TotalVolume,
            EffectiveMargin = EffectiveMargin
        };
    }
}

/// <summary>
/// Summary DTO for revenue reporting
/// </summary>
public class RevenueSummary
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PeriodType { get; set; } = "";

    // Sportsbook metrics
    public decimal SportsbookRevenue { get; set; }
    public decimal SportsbookVolume { get; set; }
    public int SportsbookBetsCount { get; set; }
    public decimal SportsbookHoldPercentage { get; set; }

    // Exchange metrics
    public decimal ExchangeRevenue { get; set; }
    public decimal ExchangeVolume { get; set; }
    public int ExchangeMatchesCount { get; set; }
    public decimal ExchangeEffectiveRate { get; set; }

    // Combined metrics
    public decimal TotalRevenue { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal EffectiveMargin { get; set; }
}
