using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Exceptions;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a sporting event (match/game) on which bets are offered
/// </summary>
public class Event
{
    public Guid Id { get; }
    public Guid LeagueId { get; }
    public string Name { get; }

    public Team HomeTeam { get; }
    public Team AwayTeam { get; }

    public DateTime ScheduledStartTime { get; }
    public string? Venue { get; }

    public EventStatus Status { get; private set; }

    /// <summary>
    /// Final score (set when event is completed)
    /// </summary>
    public Score? FinalScore { get; private set; }

    /// <summary>
    /// External ID from The Odds API (e.g., "abc123def456")
    /// Used to sync and update events from external source
    /// </summary>
    public string? ExternalId { get; private set; }

    /// <summary>
    /// Timestamp of last sync with The Odds API
    /// </summary>
    public DateTime? LastSyncedAt { get; private set; }

    private readonly List<Market> _markets;
    public IReadOnlyList<Market> Markets => _markets.AsReadOnly();

    // Private parameterless constructor for EF Core
    private Event()
    {
        // Initialize required non-nullable string properties
        Name = string.Empty;
        // Initialize value objects
        HomeTeam = null!;
        AwayTeam = null!;
        // Initialize collections
        _markets = new List<Market>();
    }

    public Event(
        string name,
        Team homeTeam,
        Team awayTeam,
        DateTime scheduledStartTime,
        Guid leagueId,
        string? venue = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Event name cannot be empty", nameof(name));
        if (homeTeam == null)
            throw new ArgumentNullException(nameof(homeTeam));
        if (awayTeam == null)
            throw new ArgumentNullException(nameof(awayTeam));
        if (homeTeam.Id == awayTeam.Id)
            throw new ArgumentException("Home and away teams must be different");

        Id = Guid.NewGuid();
        Name = name;
        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        ScheduledStartTime = scheduledStartTime;
        LeagueId = leagueId;
        Venue = venue;
        Status = EventStatus.Scheduled;
        _markets = new List<Market>();
    }

    /// <summary>
    /// Add a betting market to this event
    /// </summary>
    public void AddMarket(Market market)
    {
        if (market == null)
            throw new ArgumentNullException(nameof(market));

        if (Status == EventStatus.Cancelled)
            throw new InvalidEventStateException("Cannot add markets to a cancelled event");

        if (_markets.Any(m => m.Type == market.Type && m.Name == market.Name))
            throw new InvalidOperationException($"Market {market.Name} already exists for this event");

        market.SetEventId(Id);
        _markets.Add(market);
    }

    /// <summary>
    /// Start the event (for in-play betting)
    /// </summary>
    public void Start()
    {
        if (Status != EventStatus.Scheduled)
            throw new InvalidEventStateException($"Cannot start event in {Status} status");

        Status = EventStatus.InProgress;
    }

    /// <summary>
    /// Complete the event with final score
    /// </summary>
    public void Complete(Score finalScore)
    {
        if (Status == EventStatus.Cancelled)
            throw new InvalidEventStateException("Cannot complete a cancelled event");

        if (Status == EventStatus.Completed)
            throw new InvalidEventStateException("Event is already completed");

        FinalScore = finalScore;
        Status = EventStatus.Completed;

        // Close all markets
        foreach (var market in _markets)
        {
            if (market.IsOpen)
            {
                market.Close();
            }
        }
    }

    /// <summary>
    /// Cancel the event
    /// </summary>
    public void Cancel()
    {
        if (Status == EventStatus.Completed)
            throw new InvalidEventStateException("Cannot cancel a completed event");

        Status = EventStatus.Cancelled;

        // Close all markets
        foreach (var market in _markets)
        {
            if (market.IsOpen)
            {
                market.Close();
            }
        }
    }

    /// <summary>
    /// Suspend the event temporarily
    /// </summary>
    public void Suspend()
    {
        if (Status != EventStatus.InProgress)
            throw new InvalidEventStateException("Can only suspend an in-progress event");

        Status = EventStatus.Suspended;

        // Suspend all markets
        foreach (var market in _markets)
        {
            if (market.IsOpen)
            {
                market.Suspend();
            }
        }
    }

    /// <summary>
    /// Resume a suspended event
    /// </summary>
    public void Resume()
    {
        if (Status != EventStatus.Suspended)
            throw new InvalidEventStateException("Can only resume a suspended event");

        Status = EventStatus.InProgress;

        // Markets can be individually reopened as needed
    }

    /// <summary>
    /// Set external ID and sync timestamp (called by Odds API Listener)
    /// </summary>
    public void SetExternalId(string externalId)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External ID cannot be empty", nameof(externalId));

        ExternalId = externalId;
        LastSyncedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update sync timestamp (called after each sync from Odds API)
    /// </summary>
    public void UpdateSyncTimestamp()
    {
        LastSyncedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get a market by ID
    /// </summary>
    public Market? GetMarket(Guid marketId)
    {
        return _markets.FirstOrDefault(m => m.Id == marketId);
    }

    /// <summary>
    /// Get all markets of a specific type
    /// </summary>
    public IEnumerable<Market> GetMarketsByType(MarketType type)
    {
        return _markets.Where(m => m.Type == type);
    }

    /// <summary>
    /// Check if event has started
    /// </summary>
    public bool HasStarted => Status == EventStatus.InProgress
                              || Status == EventStatus.Completed
                              || Status == EventStatus.Suspended;

    /// <summary>
    /// Check if betting is allowed
    /// </summary>
    public bool IsBettingAllowed => Status == EventStatus.Scheduled && DateTime.UtcNow < ScheduledStartTime;

    public override string ToString() => $"{Name}: {HomeTeam} vs {AwayTeam} @ {ScheduledStartTime:g}";
}
