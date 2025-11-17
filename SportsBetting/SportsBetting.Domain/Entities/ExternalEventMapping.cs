namespace SportsBetting.Domain.Entities;

/// <summary>
/// Maps our internal Event IDs to external provider event IDs.
/// Enables reliable matching between different data sources (The Odds API, ESPN, etc.)
/// without relying on fuzzy name matching.
/// </summary>
public class ExternalEventMapping
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Reference to our internal Event
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// External provider's unique event ID (e.g., ESPN event ID, Odds API event ID)
    /// </summary>
    public string ExternalId { get; private set; }

    /// <summary>
    /// Provider name (e.g., "TheOddsApi", "ESPN", "Sportradar")
    /// </summary>
    public string Provider { get; private set; }

    /// <summary>
    /// When this mapping was first created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Last time this mapping was verified/used
    /// </summary>
    public DateTime LastVerifiedAt { get; private set; }

    /// <summary>
    /// Navigation property to the Event
    /// </summary>
    public Event Event { get; private set; } = null!;

    // EF Core constructor
    private ExternalEventMapping()
    {
        ExternalId = string.Empty;
        Provider = string.Empty;
    }

    public ExternalEventMapping(
        Guid eventId,
        string externalId,
        string provider)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("Event ID cannot be empty", nameof(eventId));

        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External ID cannot be empty", nameof(externalId));

        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider cannot be empty", nameof(provider));

        Id = Guid.NewGuid();
        EventId = eventId;
        ExternalId = externalId;
        Provider = provider;
        CreatedAt = DateTime.UtcNow;
        LastVerifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update the last verified timestamp (called when mapping is used successfully)
    /// </summary>
    public void MarkAsVerified()
    {
        LastVerifiedAt = DateTime.UtcNow;
    }
}
