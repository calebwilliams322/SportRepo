namespace SportsBetting.Domain.Entities;

/// <summary>
/// Tracks user trading statistics for commission tier calculation
/// Updated in real-time as bets are placed and settled
/// </summary>
public class UserStatistics
{
    public Guid Id { get; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    // Lifetime Statistics
    public decimal TotalVolumeAllTime { get; private set; }
    public int TotalBetsAllTime { get; private set; }
    public int TotalMatchesAllTime { get; private set; }
    public decimal TotalCommissionPaidAllTime { get; private set; }

    // 30-Day Rolling Statistics (for tier calculation)
    public decimal Volume30Day { get; private set; }
    public int Bets30Day { get; private set; }
    public int Matches30Day { get; private set; }
    public decimal Commission30Day { get; private set; }

    // 7-Day Rolling Statistics
    public decimal Volume7Day { get; private set; }
    public int Bets7Day { get; private set; }

    // Maker/Taker Statistics (liquidity provision tracking)
    public int MakerTradesAllTime { get; private set; }
    public int TakerTradesAllTime { get; private set; }
    public decimal MakerVolumeAllTime { get; private set; }
    public decimal TakerVolumeAllTime { get; private set; }

    // Performance Metrics
    public decimal NetProfitAllTime { get; private set; }
    public decimal LargestWin { get; private set; }
    public decimal LargestLoss { get; private set; }
    public DateTime? LastBetPlaced { get; private set; }
    public DateTime? LastBetSettled { get; private set; }

    // Timestamps
    public DateTime CreatedAt { get; }
    public DateTime LastUpdated { get; private set; }

    // Private constructor for EF Core
    private UserStatistics()
    {
    }

    public UserStatistics(User user)
    {
        Id = Guid.NewGuid();
        UserId = user.Id;
        User = user;
        CreatedAt = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;

        user.SetStatistics(this);
    }

    /// <summary>
    /// Record a new bet placement
    /// </summary>
    public void RecordBetPlaced(decimal stake)
    {
        TotalBetsAllTime++;
        Bets30Day++;
        Bets7Day++;
        LastBetPlaced = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Record a bet match (when bet gets matched with opposite side)
    /// </summary>
    public void RecordBetMatched(decimal matchedStake, bool isMaker)
    {
        TotalMatchesAllTime++;
        Matches30Day++;

        TotalVolumeAllTime += matchedStake;
        Volume30Day += matchedStake;
        Volume7Day += matchedStake;

        if (isMaker)
        {
            MakerTradesAllTime++;
            MakerVolumeAllTime += matchedStake;
        }
        else
        {
            TakerTradesAllTime++;
            TakerVolumeAllTime += matchedStake;
        }

        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Record commission paid on a settled bet
    /// </summary>
    public void RecordCommissionPaid(decimal commission)
    {
        TotalCommissionPaidAllTime += commission;
        Commission30Day += commission;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Record bet settlement (win/loss)
    /// </summary>
    public void RecordBetSettled(decimal profit)
    {
        NetProfitAllTime += profit;

        if (profit > LargestWin)
            LargestWin = profit;

        if (profit < 0 && Math.Abs(profit) > LargestLoss)
            LargestLoss = Math.Abs(profit);

        LastBetSettled = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Reset 30-day rolling statistics (called by background job)
    /// </summary>
    public void Reset30DayStats()
    {
        Volume30Day = 0;
        Bets30Day = 0;
        Matches30Day = 0;
        Commission30Day = 0;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Reset 7-day rolling statistics (called by background job)
    /// </summary>
    public void Reset7DayStats()
    {
        Volume7Day = 0;
        Bets7Day = 0;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Get maker percentage (what % of trades are maker vs taker)
    /// Higher percentage = more liquidity provision
    /// </summary>
    public decimal MakerPercentage
    {
        get
        {
            var totalTrades = MakerTradesAllTime + TakerTradesAllTime;
            if (totalTrades == 0) return 0;
            return (decimal)MakerTradesAllTime / totalTrades * 100;
        }
    }

    /// <summary>
    /// Get average bet size (all time)
    /// </summary>
    public decimal AverageBetSize
    {
        get
        {
            if (TotalMatchesAllTime == 0) return 0;
            return TotalVolumeAllTime / TotalMatchesAllTime;
        }
    }
}
