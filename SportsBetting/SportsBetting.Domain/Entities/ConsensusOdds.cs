namespace SportsBetting.Domain.Entities;

/// <summary>
/// Stores consensus odds data from external sources (e.g., The Odds API)
/// Used to validate user-proposed odds in exchange betting
/// </summary>
public class ConsensusOdds
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The outcome these consensus odds are for
    /// </summary>
    public Guid OutcomeId { get; private set; }
    public Outcome Outcome { get; private set; } = null!;

    /// <summary>
    /// Average odds across all bookmakers
    /// </summary>
    public decimal AverageOdds { get; private set; }

    /// <summary>
    /// Minimum odds found across bookmakers
    /// </summary>
    public decimal MinOdds { get; private set; }

    /// <summary>
    /// Maximum odds found across bookmakers
    /// </summary>
    public decimal MaxOdds { get; private set; }

    /// <summary>
    /// Number of bookmakers used in this consensus
    /// </summary>
    public int SampleSize { get; private set; }

    /// <summary>
    /// Data source (e.g., "TheOddsAPI", "Manual")
    /// </summary>
    public string Source { get; private set; }

    /// <summary>
    /// When this data was fetched
    /// </summary>
    public DateTime FetchedAt { get; private set; }

    /// <summary>
    /// When this data expires (should be refreshed)
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    // EF Core constructor
    private ConsensusOdds()
    {
        Source = string.Empty;
    }

    public ConsensusOdds(
        Guid outcomeId,
        decimal averageOdds,
        decimal minOdds,
        decimal maxOdds,
        int sampleSize,
        string source,
        TimeSpan ttl)
    {
        if (averageOdds < 1.0m)
            throw new ArgumentException("Average odds must be at least 1.0", nameof(averageOdds));

        if (minOdds < 1.0m)
            throw new ArgumentException("Min odds must be at least 1.0", nameof(minOdds));

        if (maxOdds < 1.0m)
            throw new ArgumentException("Max odds must be at least 1.0", nameof(maxOdds));

        if (minOdds > averageOdds || averageOdds > maxOdds)
            throw new ArgumentException("Odds must be ordered: min <= average <= max");

        if (sampleSize <= 0)
            throw new ArgumentException("Sample size must be positive", nameof(sampleSize));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        Id = Guid.NewGuid();
        OutcomeId = outcomeId;
        AverageOdds = averageOdds;
        MinOdds = minOdds;
        MaxOdds = maxOdds;
        SampleSize = sampleSize;
        Source = source;
        FetchedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(ttl);
    }

    /// <summary>
    /// Check if this consensus data has expired
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Calculate the percentage deviation of proposed odds from consensus
    /// </summary>
    /// <param name="proposedOdds">The odds proposed by a user</param>
    /// <returns>Deviation as a percentage (e.g., 15.5 means 15.5% deviation)</returns>
    public decimal CalculateDeviation(decimal proposedOdds)
    {
        if (AverageOdds == 0) return 100m; // Avoid division by zero

        return Math.Abs(proposedOdds - AverageOdds) / AverageOdds * 100m;
    }

    /// <summary>
    /// Check if proposed odds are within acceptable tolerance
    /// </summary>
    /// <param name="proposedOdds">The odds proposed by a user</param>
    /// <param name="tolerancePercent">Maximum allowed deviation percentage (e.g., 20 for ±20%)</param>
    /// <returns>True if within tolerance, false otherwise</returns>
    public bool IsWithinTolerance(decimal proposedOdds, decimal tolerancePercent)
    {
        return CalculateDeviation(proposedOdds) <= tolerancePercent;
    }
}
