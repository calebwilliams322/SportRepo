using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;
using SportsBettingListener.ScoreApi;
using SportsBettingListener.ScoreApi.Models;

namespace SportsBettingListener.Sync;

/// <summary>
/// Result of settling an event
/// </summary>
public record SettlementResult(int BetsSettled, decimal TotalPayout);

/// <summary>
/// Service responsible for fetching scores and auto-settling completed events.
/// Runs periodically to check for finished games and trigger settlement.
/// </summary>
public class ScoreSyncService
{
    private readonly SportsBettingDbContext _context;
    private readonly IScoreApiClient _scoreApiClient;
    private readonly SettlementService _settlementService;
    private readonly ILogger<ScoreSyncService> _logger;

    public ScoreSyncService(
        SportsBettingDbContext context,
        IScoreApiClient scoreApiClient,
        SettlementService settlementService,
        ILogger<ScoreSyncService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _scoreApiClient = scoreApiClient ?? throw new ArgumentNullException(nameof(scoreApiClient));
        _settlementService = settlementService ?? throw new ArgumentNullException(nameof(settlementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Check for completed games and auto-settle them.
    /// Called periodically by the background worker.
    /// </summary>
    public async Task CheckAndSettleCompletedGamesAsync(List<string> sports, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Checking for completed games to settle ===");

        var totalSettled = 0;
        var totalErrors = 0;

        foreach (var sport in sports)
        {
            try
            {
                var (settled, errors) = await CheckSportAsync(sport, cancellationToken);
                totalSettled += settled;
                totalErrors += errors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check scores for {Sport}", sport);
                totalErrors++;
            }
        }

        if (totalSettled > 0)
        {
            _logger.LogInformation("=== Auto-settlement complete: {Settled} events settled, {Errors} errors ===",
                totalSettled, totalErrors);
        }
        else
        {
            _logger.LogDebug("No completed events found to settle");
        }
    }

    /// <summary>
    /// Check a single sport for completed games
    /// </summary>
    private async Task<(int settled, int errors)> CheckSportAsync(string sport, CancellationToken cancellationToken)
    {
        if (!_scoreApiClient.SupportsSport(sport))
        {
            _logger.LogDebug("Skipping {Sport} - not supported by score provider", sport);
            return (0, 0);
        }

        // Fetch current scores from ESPN
        var scoreEvents = await _scoreApiClient.FetchScoresAsync(sport, cancellationToken);

        _logger.LogDebug("Fetched {Count} score events for {Sport}", scoreEvents.Count, sport);

        var settledCount = 0;
        var errorCount = 0;

        // Check each completed game
        foreach (var scoreEvent in scoreEvents.Where(e => e.IsCompleted))
        {
            try
            {
                var wasSettled = await TrySettleEventAsync(scoreEvent, cancellationToken);
                if (wasSettled)
                    settledCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to settle event {HomeTeam} vs {AwayTeam}",
                    scoreEvent.HomeTeamName, scoreEvent.AwayTeamName);
                errorCount++;
            }
        }

        return (settledCount, errorCount);
    }

    /// <summary>
    /// Try to find and settle an event based on score data
    /// </summary>
    private async Task<bool> TrySettleEventAsync(ScoreEvent scoreEvent, CancellationToken cancellationToken)
    {
        if (!scoreEvent.HomeScore.HasValue || !scoreEvent.AwayScore.HasValue)
        {
            _logger.LogWarning("Cannot settle event {Home} vs {Away} - scores missing",
                scoreEvent.HomeTeamName, scoreEvent.AwayTeamName);
            return false;
        }

        // Find matching event in our database
        var matchingEvent = await FindMatchingEventAsync(scoreEvent, cancellationToken);

        if (matchingEvent == null)
        {
            _logger.LogDebug("No matching event found for {Home} vs {Away}",
                scoreEvent.HomeTeamName, scoreEvent.AwayTeamName);
            return false;
        }

        // Check if already settled
        if (matchingEvent.Status == EventStatus.Completed)
        {
            _logger.LogDebug("Event {EventId} already settled", matchingEvent.Id);
            return false;
        }

        // Settle the event!
        _logger.LogInformation("Auto-settling event {EventId}: {Home} {HomeScore} - {Away} {AwayScore}",
            matchingEvent.Id,
            scoreEvent.HomeTeamName, scoreEvent.HomeScore,
            scoreEvent.AwayTeamName, scoreEvent.AwayScore);

        var result = await SettleEventWithScoreAsync(
            matchingEvent,
            scoreEvent.HomeScore.Value,
            scoreEvent.AwayScore.Value,
            cancellationToken);

        _logger.LogInformation("Successfully settled event {EventId}: {BetCount} bets, ${TotalPayout} paid out",
            matchingEvent.Id, result.BetsSettled, result.TotalPayout);

        return true;
    }

    /// <summary>
    /// Complete and settle an event with final scores, then settle all related bets
    /// </summary>
    private async Task<SettlementResult> SettleEventWithScoreAsync(
        Event evt,
        int homeScore,
        int awayScore,
        CancellationToken cancellationToken)
    {
        // Create the final score
        var finalScore = new Score(homeScore, awayScore);

        // Complete the event (sets final score, closes markets)
        evt.Complete(finalScore);

        // Settle all markets in the event (determines winning outcomes)
        _settlementService.SettleEvent(evt);

        // Find all sportsbook bets for this event that need settlement
        var betsToSettle = await _context.Bets
            .Include(b => b.Selections)
            .Where(b => b.Selections.Any(s => s.EventId == evt.Id))
            .Where(b => b.Status == BetStatus.Pending) // Only pending bets
            .Where(b => b.BetMode == BetMode.Sportsbook) // Only sportsbook bets (exchange handled separately)
            .ToListAsync(cancellationToken);

        decimal totalPayout = 0;
        int settledCount = 0;

        // Settle each bet
        foreach (var bet in betsToSettle)
        {
            try
            {
                _settlementService.SettleBet(bet, new[] { evt });

                if (bet.Status == BetStatus.Won && bet.ActualPayout.HasValue)
                {
                    totalPayout += bet.ActualPayout.Value.Amount;
                }

                settledCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to settle bet {BetId} for event {EventId}",
                    bet.Id, evt.Id);
            }
        }

        // Save all changes
        await _context.SaveChangesAsync(cancellationToken);

        return new SettlementResult(settledCount, totalPayout);
    }

    /// <summary>
    /// Find event in database that matches the score event.
    /// First tries ID-based matching, then falls back to team name matching.
    /// </summary>
    private async Task<Event?> FindMatchingEventAsync(ScoreEvent scoreEvent, CancellationToken cancellationToken)
    {
        // STEP 1: Try to find by ESPN external ID (most reliable - 100% confidence)
        var existingMapping = await _context.ExternalEventMappings
            .Include(m => m.Event)
                .ThenInclude(e => e.HomeTeam)
            .Include(m => m.Event)
                .ThenInclude(e => e.AwayTeam)
            .Include(m => m.Event)
                .ThenInclude(e => e.Markets)
                    .ThenInclude(m => m.Outcomes)
            .FirstOrDefaultAsync(
                m => m.ExternalId == scoreEvent.ExternalId && m.Provider == "ESPN",
                cancellationToken);

        if (existingMapping != null)
        {
            existingMapping.MarkAsVerified();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✓ Found event by ESPN ID mapping (100% confidence): Event {EventId}, ESPN ID {EspnId}",
                existingMapping.EventId, scoreEvent.ExternalId);

            return existingMapping.Event;
        }

        // STEP 2: Fall back to fuzzy name matching (lower confidence)
        _logger.LogDebug("No ESPN ID mapping found for {EspnId}, trying fuzzy name matching...",
            scoreEvent.ExternalId);

        var eventDate = scoreEvent.EventDate.Date;
        var startOfDay = eventDate;
        var endOfDay = eventDate.AddDays(1);

        var candidateEvents = await _context.Events
            .Include(e => e.HomeTeam)
            .Include(e => e.AwayTeam)
            .Include(e => e.Markets)
                .ThenInclude(m => m.Outcomes)
            .Where(e => e.ScheduledStartTime >= startOfDay && e.ScheduledStartTime < endOfDay)
            .Where(e => e.Status == EventStatus.Scheduled || e.Status == EventStatus.InProgress)
            .ToListAsync(cancellationToken);

        if (candidateEvents.Count == 0)
        {
            _logger.LogWarning("No candidate events found for date {Date}", eventDate);
            return null;
        }

        // Calculate match confidence for each candidate
        var bestMatch = candidateEvents
            .Select(e => new
            {
                Event = e,
                Confidence = CalculateMatchConfidence(e, scoreEvent)
            })
            .OrderByDescending(m => m.Confidence)
            .FirstOrDefault();

        if (bestMatch == null || bestMatch.Confidence < 0.7f)
        {
            _logger.LogWarning("No confident match found for {Home} vs {Away}. Best confidence: {Confidence:P0}",
                scoreEvent.HomeTeamName, scoreEvent.AwayTeamName, bestMatch?.Confidence ?? 0);
            return null;
        }

        _logger.LogInformation("⚠ Fuzzy matched event {EventId} with {Confidence:P0} confidence: {OurHome} vs {OurAway} ≈ {EspnHome} vs {EspnAway}",
            bestMatch.Event.Id, bestMatch.Confidence,
            bestMatch.Event.HomeTeam.Name, bestMatch.Event.AwayTeam.Name,
            scoreEvent.HomeTeamName, scoreEvent.AwayTeamName);

        // STEP 3: Save ESPN mapping for future use (learning from fuzzy match)
        var espnMapping = new ExternalEventMapping(
            eventId: bestMatch.Event.Id,
            externalId: scoreEvent.ExternalId,
            provider: "ESPN");
        _context.ExternalEventMappings.Add(espnMapping);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("✓ Saved ESPN ID mapping for future lookups: Event {EventId} → ESPN {EspnId}",
            bestMatch.Event.Id, scoreEvent.ExternalId);

        return bestMatch.Event;
    }

    /// <summary>
    /// Calculate confidence score (0.0 to 1.0) for how well an event matches a score event.
    /// Higher score = better match.
    /// </summary>
    private float CalculateMatchConfidence(Event evt, ScoreEvent scoreEvent)
    {
        float confidence = 0f;

        // Check home team match (0.5 points available)
        if (evt.HomeTeam.Name.Equals(scoreEvent.HomeTeamName, StringComparison.OrdinalIgnoreCase))
            confidence += 0.5f; // Exact match
        else if (TeamNamesMatch(evt.HomeTeam.Name, scoreEvent.HomeTeamName))
            confidence += 0.3f; // Fuzzy match

        // Check away team match (0.5 points available)
        if (evt.AwayTeam.Name.Equals(scoreEvent.AwayTeamName, StringComparison.OrdinalIgnoreCase))
            confidence += 0.5f; // Exact match
        else if (TeamNamesMatch(evt.AwayTeam.Name, scoreEvent.AwayTeamName))
            confidence += 0.3f; // Fuzzy match

        return confidence;
    }

    /// <summary>
    /// Check if team names match (handles variations like "Chiefs" vs "Kansas City Chiefs")
    /// </summary>
    private bool TeamNamesMatch(string ourTeamName, string espnTeamName)
    {
        if (ourTeamName.Equals(espnTeamName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if ESPN name contains our team name (e.g., "Kansas City Chiefs" contains "Chiefs")
        if (espnTeamName.Contains(ourTeamName, StringComparison.OrdinalIgnoreCase))
            return true;

        // Check reverse (our name contains ESPN name)
        if (ourTeamName.Contains(espnTeamName, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
