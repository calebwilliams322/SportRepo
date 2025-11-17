using Microsoft.AspNetCore.SignalR;
using SportsBetting.API.DTOs;
using System.Security.Claims;

namespace SportsBetting.API.Hubs;

/// <summary>
/// SignalR Hub for real-time order book and betting updates
/// Manages WebSocket connections and message broadcasting
/// </summary>
public class OrderBookHub : Hub
{
    private readonly ILogger<OrderBookHub> _logger;

    public OrderBookHub(ILogger<OrderBookHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when client connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        _logger.LogInformation(
            "Client connected: ConnectionId={ConnectionId}, UserId={UserId}",
            connectionId,
            userId ?? "Anonymous"
        );

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when client disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Client disconnected with error: ConnectionId={ConnectionId}, UserId={UserId}",
                connectionId,
                userId ?? "Anonymous"
            );
        }
        else
        {
            _logger.LogInformation(
                "Client disconnected: ConnectionId={ConnectionId}, UserId={UserId}",
                connectionId,
                userId ?? "Anonymous"
            );
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to order book updates for a specific outcome
    /// </summary>
    /// <param name="outcomeId">The outcome ID to watch</param>
    public async Task SubscribeToOutcome(string outcomeId)
    {
        if (!Guid.TryParse(outcomeId, out var parsedId))
        {
            _logger.LogWarning(
                "Invalid outcome ID format: {OutcomeId} from ConnectionId={ConnectionId}",
                outcomeId,
                Context.ConnectionId
            );
            return;
        }

        var groupName = $"outcome-{parsedId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client subscribed to outcome: OutcomeId={OutcomeId}, ConnectionId={ConnectionId}",
            parsedId,
            Context.ConnectionId
        );
    }

    /// <summary>
    /// Unsubscribe from order book updates for a specific outcome
    /// </summary>
    /// <param name="outcomeId">The outcome ID to stop watching</param>
    public async Task UnsubscribeFromOutcome(string outcomeId)
    {
        if (!Guid.TryParse(outcomeId, out var parsedId))
        {
            return;
        }

        var groupName = $"outcome-{parsedId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client unsubscribed from outcome: OutcomeId={OutcomeId}, ConnectionId={ConnectionId}",
            parsedId,
            Context.ConnectionId
        );
    }

    /// <summary>
    /// Subscribe to all updates for a specific market
    /// </summary>
    /// <param name="marketId">The market ID to watch</param>
    public async Task SubscribeToMarket(string marketId)
    {
        if (!Guid.TryParse(marketId, out var parsedId))
        {
            _logger.LogWarning(
                "Invalid market ID format: {MarketId} from ConnectionId={ConnectionId}",
                marketId,
                Context.ConnectionId
            );
            return;
        }

        var groupName = $"market-{parsedId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client subscribed to market: MarketId={MarketId}, ConnectionId={ConnectionId}",
            parsedId,
            Context.ConnectionId
        );
    }

    /// <summary>
    /// Unsubscribe from market updates
    /// </summary>
    /// <param name="marketId">The market ID to stop watching</param>
    public async Task UnsubscribeFromMarket(string marketId)
    {
        if (!Guid.TryParse(marketId, out var parsedId))
        {
            return;
        }

        var groupName = $"market-{parsedId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client unsubscribed from market: MarketId={MarketId}, ConnectionId={ConnectionId}",
            parsedId,
            Context.ConnectionId
        );
    }

    /// <summary>
    /// Subscribe to user-specific notifications (bet matches, etc.)
    /// Automatically uses the authenticated user's ID
    /// </summary>
    public async Task SubscribeToUserNotifications()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning(
                "Cannot subscribe to user notifications - not authenticated: ConnectionId={ConnectionId}",
                Context.ConnectionId
            );
            return;
        }

        var groupName = $"user-{userId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client subscribed to user notifications: UserId={UserId}, ConnectionId={ConnectionId}",
            userId,
            Context.ConnectionId
        );
    }

    /// <summary>
    /// Subscribe to event updates
    /// </summary>
    /// <param name="eventId">The event ID to watch</param>
    public async Task SubscribeToEvent(string eventId)
    {
        if (!Guid.TryParse(eventId, out var parsedId))
        {
            _logger.LogWarning(
                "Invalid event ID format: {EventId} from ConnectionId={ConnectionId}",
                eventId,
                Context.ConnectionId
            );
            return;
        }

        var groupName = $"event-{parsedId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client subscribed to event: EventId={EventId}, ConnectionId={ConnectionId}",
            parsedId,
            Context.ConnectionId
        );
    }

    /// <summary>
    /// Unsubscribe from event updates
    /// </summary>
    /// <param name="eventId">The event ID to stop watching</param>
    public async Task UnsubscribeFromEvent(string eventId)
    {
        if (!Guid.TryParse(eventId, out var parsedId))
        {
            return;
        }

        var groupName = $"event-{parsedId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Client unsubscribed from event: EventId={EventId}, ConnectionId={ConnectionId}",
            parsedId,
            Context.ConnectionId
        );
    }

    /// <summary>
    /// Heartbeat/ping method for connection keep-alive
    /// </summary>
    public Task Ping()
    {
        return Task.CompletedTask;
    }
}
