using SportsBetting.Domain.Enums;

namespace SportsBetting.API.DTOs;

/// <summary>
/// Base class for all WebSocket messages
/// </summary>
public abstract class WebSocketMessage
{
    public string Type { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Order book update message - sent when orders are placed, matched, or cancelled
/// </summary>
public class OrderBookUpdateMessage : WebSocketMessage
{
    public OrderBookUpdateMessage()
    {
        Type = "ORDER_BOOK_UPDATE";
    }

    public Guid OutcomeId { get; init; }
    public List<OrderBookEntry> BackOrders { get; init; } = new();
    public List<OrderBookEntry> LayOrders { get; init; } = new();
    public decimal? ConsensusBackOdds { get; init; }
    public decimal? ConsensusLayOdds { get; init; }
}

/// <summary>
/// Individual order in the order book
/// </summary>
public class OrderBookEntry
{
    public decimal Odds { get; init; }
    public decimal Stake { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Username { get; init; } // Optional - may hide for privacy
}

/// <summary>
/// Bet matched notification - sent to user when their bet gets matched
/// </summary>
public class BetMatchedMessage : WebSocketMessage
{
    public BetMatchedMessage()
    {
        Type = "BET_MATCHED";
    }

    public Guid BetId { get; init; }
    public Guid ExchangeBetId { get; init; }
    public decimal MatchedAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public decimal MatchedOdds { get; init; }
    public bool FullyMatched { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Odds update message - sent when consensus odds change
/// </summary>
public class OddsUpdateMessage : WebSocketMessage
{
    public OddsUpdateMessage()
    {
        Type = "ODDS_UPDATE";
    }

    public Guid OutcomeId { get; init; }
    public string OutcomeName { get; init; } = string.Empty;
    public decimal? BackOdds { get; init; }
    public decimal? LayOdds { get; init; }
    public decimal? PreviousBackOdds { get; init; }
    public decimal? PreviousLayOdds { get; init; }
    public OddsTrend Trend { get; init; }
}

/// <summary>
/// Market status change - sent when market opens, closes, or is suspended
/// </summary>
public class MarketStatusMessage : WebSocketMessage
{
    public MarketStatusMessage()
    {
        Type = "MARKET_STATUS";
    }

    public Guid MarketId { get; init; }
    public string MarketName { get; init; } = string.Empty;
    public MarketStatus Status { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event update - sent when event starts, ends, or status changes
/// </summary>
public class EventUpdateMessage : WebSocketMessage
{
    public EventUpdateMessage()
    {
        Type = "EVENT_UPDATE";
    }

    public Guid EventId { get; init; }
    public string EventName { get; init; } = string.Empty;
    public Domain.Enums.EventStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// New order placed notification - sent to all watching order book
/// </summary>
public class NewOrderMessage : WebSocketMessage
{
    public NewOrderMessage()
    {
        Type = "NEW_ORDER";
    }

    public Guid OutcomeId { get; init; }
    public BetSide Side { get; init; }
    public decimal Odds { get; init; }
    public decimal Stake { get; init; }
}

/// <summary>
/// Order cancelled notification
/// </summary>
public class OrderCancelledMessage : WebSocketMessage
{
    public OrderCancelledMessage()
    {
        Type = "ORDER_CANCELLED";
    }

    public Guid OutcomeId { get; init; }
    public Guid ExchangeBetId { get; init; }
    public BetSide Side { get; init; }
    public decimal CancelledStake { get; init; }
}

/// <summary>
/// Odds trend enum (specific to WebSocket messages)
/// </summary>
public enum OddsTrend
{
    Up,
    Down,
    Stable
}

/// <summary>
/// Market status enum (specific to WebSocket messages)
/// </summary>
public enum MarketStatus
{
    Open,
    Suspended,
    Closed,
    Settled
}
