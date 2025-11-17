using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Historical record of odds changes for an outcome.
/// Tracks every odds update from The Odds API for analytics and compliance.
/// </summary>
public class OddsHistory
{
    public Guid Id { get; private set; }

    /// <summary>
    /// The outcome these odds apply to
    /// </summary>
    public Guid OutcomeId { get; private set; }

    /// <summary>
    /// Navigation property to the outcome
    /// </summary>
    public Outcome? Outcome { get; private set; }

    /// <summary>
    /// The odds value at this point in time
    /// </summary>
    public Odds Odds { get; private set; }

    /// <summary>
    /// Source of the odds (e.g., "DraftKings", "Consensus", "FanDuel")
    /// </summary>
    public string Source { get; private set; }

    /// <summary>
    /// When these odds were recorded
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Optional: Raw JSON data from all bookmakers for this outcome at this time
    /// Useful for detailed analysis or validation
    /// </summary>
    public string? RawBookmakerData { get; private set; }

    // Private parameterless constructor for EF Core
    private OddsHistory()
    {
        Source = string.Empty;
    }

    public OddsHistory(
        Guid outcomeId,
        Odds odds,
        string source,
        string? rawBookmakerData = null)
    {
        if (outcomeId == Guid.Empty)
            throw new ArgumentException("Outcome ID cannot be empty", nameof(outcomeId));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be empty", nameof(source));

        Id = Guid.NewGuid();
        OutcomeId = outcomeId;
        Odds = odds;
        Source = source;
        Timestamp = DateTime.UtcNow;
        RawBookmakerData = rawBookmakerData;
    }

    public override string ToString() =>
        $"{Source} @ {Timestamp:yyyy-MM-dd HH:mm:ss}: {Odds}";
}
