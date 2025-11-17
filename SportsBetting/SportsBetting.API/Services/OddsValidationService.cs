using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Services;

namespace SportsBetting.API.Services;

/// <summary>
/// Service for validating user-proposed odds against market consensus
/// Uses caching for performance
/// </summary>
public class OddsValidationService : IOddsValidationService
{
    private readonly SportsBettingDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OddsValidationService> _logger;

    public OddsValidationService(
        SportsBettingDbContext context,
        IMemoryCache cache,
        ILogger<OddsValidationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Validate proposed odds against consensus
    /// Returns warning if outside tolerance (but always allows bet)
    /// </summary>
    public async Task<OddsValidationResult> ValidateOddsAsync(
        Guid outcomeId,
        decimal proposedOdds,
        decimal tolerancePercent = 20.0m)
    {
        var consensus = await GetConsensusOddsAsync(outcomeId);

        if (consensus == null || consensus.IsExpired())
        {
            _logger.LogWarning(
                "No valid consensus odds for outcome {OutcomeId}", outcomeId);

            return new OddsValidationResult
            {
                IsValid = true, // Allow bet even without consensus
                ProposedOdds = proposedOdds,
                Message = "No consensus data available - proceeding with caution",
                HasWarning = true
            };
        }

        var deviation = consensus.CalculateDeviation(proposedOdds);
        bool withinTolerance = consensus.IsWithinTolerance(proposedOdds, tolerancePercent);

        return new OddsValidationResult
        {
            IsValid = true, // Always allow (per user requirement: warnings only, no blocking)
            ConsensusOdds = consensus.AverageOdds,
            ProposedOdds = proposedOdds,
            DeviationPercent = deviation,
            HasWarning = !withinTolerance,
            Message = withinTolerance
                ? "Odds within acceptable range"
                : $"⚠️  Warning: Odds deviate {deviation:F1}% from market consensus " +
                  $"(average: {consensus.AverageOdds:F2}). Maximum recommended deviation is {tolerancePercent}%."
        };
    }

    /// <summary>
    /// Get consensus odds for an outcome (with caching)
    /// </summary>
    public async Task<ConsensusOdds?> GetConsensusOddsAsync(Guid outcomeId)
    {
        var cacheKey = $"consensus_odds_{outcomeId}";

        if (_cache.TryGetValue(cacheKey, out ConsensusOdds? cached))
        {
            return cached;
        }

        var consensus = await _context.ConsensusOdds
            .Where(co => co.OutcomeId == outcomeId)
            .Where(co => co.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(co => co.FetchedAt)
            .FirstOrDefaultAsync();

        if (consensus != null)
        {
            var ttl = consensus.ExpiresAt - DateTime.UtcNow;
            _cache.Set(cacheKey, consensus, ttl);
        }

        return consensus;
    }

    /// <summary>
    /// Refresh consensus odds for an event
    /// TODO: Implement The Odds API integration
    /// </summary>
    public async Task RefreshConsensusOddsAsync(Guid eventId)
    {
        // Placeholder for The Odds API integration
        // This would:
        // 1. Call The Odds API for the event
        // 2. Parse the response
        // 3. Create ConsensusOdds records for each outcome
        // 4. Save to database
        // 5. Clear cache for affected outcomes

        _logger.LogInformation(
            "Refreshing consensus odds for event {EventId} - placeholder (The Odds API integration pending)",
            eventId);

        await Task.CompletedTask;

        // Example of what the implementation would look like:
        /*
        var evt = await _context.Events
            .Include(e => e.Markets)
                .ThenInclude(m => m.Outcomes)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (evt == null)
            throw new ArgumentException("Event not found");

        // Call The Odds API
        var oddsApiData = await _theOddsApiClient.GetOddsAsync(evt.ExternalId);

        // For each outcome, create consensus odds
        foreach (var outcome in evt.Markets.SelectMany(m => m.Outcomes))
        {
            var oddsForOutcome = oddsApiData.Bookmakers
                .Select(b => b.GetOddsFor(outcome.Name))
                .Where(o => o != null)
                .ToList();

            if (oddsForOutcome.Any())
            {
                var consensus = new ConsensusOdds(
                    outcome.Id,
                    averageOdds: oddsForOutcome.Average(),
                    minOdds: oddsForOutcome.Min(),
                    maxOdds: oddsForOutcome.Max(),
                    sampleSize: oddsForOutcome.Count,
                    source: "TheOddsAPI",
                    ttl: TimeSpan.FromMinutes(5)
                );

                _context.ConsensusOdds.Add(consensus);

                // Clear cache
                _cache.Remove($"consensus_odds_{outcome.Id}");
            }
        }

        await _context.SaveChangesAsync();
        */
    }
}
