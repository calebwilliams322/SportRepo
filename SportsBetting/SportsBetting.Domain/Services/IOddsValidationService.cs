using SportsBetting.Domain.Entities;

namespace SportsBetting.Domain.Services;

/// <summary>
/// Service for validating user-proposed odds against market consensus
/// Uses 20% tolerance with warnings (per user requirement)
/// </summary>
public interface IOddsValidationService
{
    /// <summary>
    /// Validate proposed odds against consensus data
    /// Returns warning if outside tolerance, but doesn't block
    /// </summary>
    /// <param name="outcomeId">The outcome being bet on</param>
    /// <param name="proposedOdds">The odds proposed by the user</param>
    /// <param name="tolerancePercent">Maximum allowed deviation (default: 20%)</param>
    /// <returns>Validation result with warning if outside tolerance</returns>
    Task<OddsValidationResult> ValidateOddsAsync(
        Guid outcomeId,
        decimal proposedOdds,
        decimal tolerancePercent = 20.0m);

    /// <summary>
    /// Get consensus odds for an outcome
    /// </summary>
    /// <param name="outcomeId">The outcome to get consensus for</param>
    /// <returns>Consensus odds if available, null otherwise</returns>
    Task<ConsensusOdds?> GetConsensusOddsAsync(Guid outcomeId);

    /// <summary>
    /// Refresh consensus odds for an event from external API
    /// (e.g., The Odds API)
    /// </summary>
    /// <param name="eventId">The event to refresh odds for</param>
    Task RefreshConsensusOddsAsync(Guid eventId);
}

/// <summary>
/// Result of odds validation
/// </summary>
public class OddsValidationResult
{
    /// <summary>
    /// Whether the odds are valid (within tolerance or no consensus available)
    /// Always true with current configuration (warnings only, no blocking)
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// The consensus odds from the market
    /// </summary>
    public decimal ConsensusOdds { get; set; }

    /// <summary>
    /// The odds proposed by the user
    /// </summary>
    public decimal ProposedOdds { get; set; }

    /// <summary>
    /// Percentage deviation from consensus (e.g., 15.5 = 15.5% deviation)
    /// </summary>
    public decimal DeviationPercent { get; set; }

    /// <summary>
    /// Human-readable reason/warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether there's a warning (odds outside tolerance)
    /// </summary>
    public bool HasWarning { get; set; }
}
