namespace SportsBetting.API.DTOs;

/// <summary>
/// Response DTO for event information
/// </summary>
public record EventResponse(
    Guid Id,
    string Name,
    Guid LeagueId,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    DateTime ScheduledStartTime,
    string? Venue,
    string Status,
    string? FinalScore,
    List<MarketSummary> Markets
);

/// <summary>
/// Summary of a market (for inclusion in EventResponse)
/// </summary>
public record MarketSummary(
    Guid Id,
    string Type,
    string Name,
    bool IsOpen,
    int OutcomeCount
);

/// <summary>
/// Detailed market response with outcomes
/// </summary>
public record MarketResponse(
    Guid Id,
    Guid EventId,
    string Type,
    string Name,
    string? Description,
    bool IsOpen,
    bool IsSettled,
    List<OutcomeResponse> Outcomes
);

/// <summary>
/// Response DTO for outcome
/// </summary>
public record OutcomeResponse(
    Guid Id,
    Guid MarketId,
    string Name,
    string Description,
    decimal CurrentOdds,
    decimal? Line,
    bool? IsWinner,
    bool IsVoid
);

/// <summary>
/// Request DTO for updating odds
/// </summary>
public record UpdateOddsRequest(
    decimal NewOdds
);

/// <summary>
/// Request DTO for creating an event
/// </summary>
public record CreateEventRequest(
    string Name,
    Guid LeagueId,
    Guid HomeTeamId,
    Guid AwayTeamId,
    DateTime ScheduledStartTime,
    string? Venue
);

/// <summary>
/// Request DTO for completing an event
/// </summary>
public record CompleteEventRequest(
    int HomeScore,
    int AwayScore
);
