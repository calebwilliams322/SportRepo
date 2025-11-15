namespace SportsBetting.API.DTOs;

/// <summary>
/// Response DTO for bet information
/// </summary>
public record BetResponse(
    Guid Id,
    Guid UserId,
    string TicketNumber,
    string Type,
    string Status,
    decimal Stake,
    string Currency,
    decimal CombinedOdds,
    decimal PotentialPayout,
    decimal? ActualPayout,
    DateTime PlacedAt,
    DateTime? SettledAt,
    List<BetSelectionResponse> Selections
);

/// <summary>
/// Response DTO for bet selection
/// </summary>
public record BetSelectionResponse(
    Guid Id,
    Guid EventId,
    string EventName,
    Guid MarketId,
    string MarketType,
    string MarketName,
    Guid OutcomeId,
    string OutcomeName,
    decimal LockedOdds,
    decimal? Line,
    string Result
);

/// <summary>
/// Request DTO for placing a single bet
/// </summary>
public record PlaceBetRequest(
    Guid EventId,
    Guid MarketId,
    Guid OutcomeId,
    decimal Stake
);

/// <summary>
/// Request DTO for placing a parlay bet
/// </summary>
public record PlaceParlayBetRequest(
    List<BetLeg> Legs,
    decimal Stake
);

/// <summary>
/// Represents one leg of a parlay bet
/// </summary>
public record BetLeg(
    Guid EventId,
    Guid MarketId,
    Guid OutcomeId
);
