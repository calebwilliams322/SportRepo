using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBettingListener.OddsApi.Models;
using SportsBettingListener.Sync.Mappers;

namespace SportsBettingListener.Sync;

/// <summary>
/// Service responsible for synchronizing events from The Odds API to the database.
/// Handles both creating new events and updating existing ones with latest odds.
/// </summary>
public class EventSyncService
{
    private readonly SportsBettingDbContext _context;
    private readonly EventMapper _eventMapper;
    private readonly MarketMapper _marketMapper;
    private readonly ILogger<EventSyncService> _logger;

    public EventSyncService(
        SportsBettingDbContext context,
        EventMapper eventMapper,
        MarketMapper marketMapper,
        ILogger<EventSyncService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventMapper = eventMapper ?? throw new ArgumentNullException(nameof(eventMapper));
        _marketMapper = marketMapper ?? throw new ArgumentNullException(nameof(marketMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Synchronizes a single event from The Odds API.
    /// Creates the event if new, or updates odds if it already exists.
    /// </summary>
    public async Task SyncEventAsync(OddsApiEvent oddsEvent, CancellationToken cancellationToken = default)
    {
        if (oddsEvent == null)
            throw new ArgumentNullException(nameof(oddsEvent));

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Syncing event: {HomeTeam} vs {AwayTeam} (External ID: {ExternalId})",
                oddsEvent.HomeTeam, oddsEvent.AwayTeam, oddsEvent.Id);

            // Check if event already exists by external ID
            var existingEvent = await _context.Events
                .Include(e => e.Markets)
                    .ThenInclude(m => m.Outcomes)
                .FirstOrDefaultAsync(e => e.ExternalId == oddsEvent.Id, cancellationToken);

            if (existingEvent == null)
            {
                // New event - create everything
                await CreateNewEventAsync(oddsEvent, cancellationToken);
            }
            else
            {
                // Existing event - update odds
                await UpdateEventOddsAsync(existingEvent, oddsEvent, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Successfully synced event {ExternalId}", oddsEvent.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            // Clear change tracker to prevent stale entities from interfering with next event
            _context.ChangeTracker.Clear();

            _logger.LogError(ex, "Failed to sync event {ExternalId}: {HomeTeam} vs {AwayTeam}",
                oddsEvent.Id, oddsEvent.HomeTeam, oddsEvent.AwayTeam);
            throw;
        }
    }

    /// <summary>
    /// Synchronizes multiple events in batch.
    /// More efficient than syncing one at a time.
    /// </summary>
    public async Task SyncEventsAsync(List<OddsApiEvent> oddsEvents, CancellationToken cancellationToken = default)
    {
        if (oddsEvents == null || oddsEvents.Count == 0)
        {
            _logger.LogWarning("No events to sync");
            return;
        }

        _logger.LogInformation("Starting batch sync of {Count} events", oddsEvents.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var oddsEvent in oddsEvents)
        {
            try
            {
                await SyncEventAsync(oddsEvent, cancellationToken);
                successCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync event in batch: {ExternalId}", oddsEvent.Id);
                failCount++;
                // Continue with next event instead of failing entire batch
            }
        }

        _logger.LogInformation("Batch sync completed: {Success} succeeded, {Failed} failed",
            successCount, failCount);
    }

    /// <summary>
    /// Creates a new event with all its markets and outcomes.
    /// Adds all entities to context then saves once (within the transaction).
    /// </summary>
    private async Task CreateNewEventAsync(OddsApiEvent oddsEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new event: {HomeTeam} vs {AwayTeam}",
            oddsEvent.HomeTeam, oddsEvent.AwayTeam);

        // 1. Map the event entity (this creates Sport, League, Teams if needed)
        var evt = await _eventMapper.MapToEventAsync(oddsEvent, cancellationToken);

        // 2. Map markets and add them to the event
        var markets = _marketMapper.MapMarketsForEvent(oddsEvent);
        foreach (var market in markets)
        {
            evt.AddMarket(market);
        }

        // 3. Add event to context (this will cascade to markets and outcomes)
        _context.Events.Add(evt);

        // 4. Create external event mapping for The Odds API
        var oddsApiMapping = new ExternalEventMapping(
            eventId: evt.Id,
            externalId: oddsEvent.Id,
            provider: "TheOddsApi");
        _context.ExternalEventMappings.Add(oddsApiMapping);

        // 5. Save everything in one transaction
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created event {EventId} with {MarketCount} markets and {OutcomeCount} total outcomes",
            evt.Id, markets.Count, markets.Sum(m => m.Outcomes.Count));
    }

    /// <summary>
    /// Updates odds for an existing event.
    /// Does not create new markets, only updates existing ones.
    /// </summary>
    private async Task UpdateEventOddsAsync(Event existingEvent, OddsApiEvent oddsEvent, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating odds for existing event {EventId} (External ID: {ExternalId})",
            existingEvent.Id, oddsEvent.Id);

        // Get the preferred bookmaker
        var bookmaker = oddsEvent.Bookmakers
            .FirstOrDefault(b => b.Key.Equals("draftkings", StringComparison.OrdinalIgnoreCase))
            ?? oddsEvent.Bookmakers.FirstOrDefault();

        if (bookmaker == null)
        {
            _logger.LogWarning("No bookmakers available for event update {ExternalId}", oddsEvent.Id);
            return;
        }

        var updatedCount = 0;
        var historyRecords = new List<OddsHistory>();

        // Update odds for each market
        foreach (var oddsMarket in bookmaker.Markets)
        {
            // Find the corresponding market by external ID
            var market = existingEvent.Markets
                .FirstOrDefault(m => m.ExternalId == oddsMarket.Key);

            if (market != null)
            {
                _marketMapper.UpdateMarketOdds(market, oddsMarket);
                updatedCount++;

                // Save odds history for each outcome in this market
                foreach (var outcome in market.Outcomes)
                {
                    var oddsHistory = new OddsHistory(
                        outcomeId: outcome.Id,
                        odds: outcome.CurrentOdds,
                        source: bookmaker.Title, // e.g., "DraftKings"
                        rawBookmakerData: null // Could serialize all bookmaker data here if needed
                    );

                    historyRecords.Add(oddsHistory);
                }
            }
            else
            {
                _logger.LogWarning("Market {MarketKey} not found for event {EventId}", oddsMarket.Key, existingEvent.Id);
            }
        }

        // Update sync timestamp on the event
        existingEvent.UpdateSyncTimestamp();

        // Save odds history records
        if (historyRecords.Count > 0)
        {
            _context.OddsHistory.AddRange(historyRecords);
            _logger.LogDebug("Saved {Count} odds history records for event {EventId}",
                historyRecords.Count, existingEvent.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated {Count} markets with {HistoryCount} odds history records for event {EventId}",
            updatedCount, historyRecords.Count, existingEvent.Id);
    }
}
