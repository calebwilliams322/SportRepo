using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;

namespace SportsBetting.Domain.Services;

/// <summary>
/// Service for matching exchange bets using FIFO algorithm
/// </summary>
public interface IBetMatchingService
{
    /// <summary>
    /// Try to match a new exchange bet with existing unmatched bets
    /// Uses FIFO algorithm: best odds first, then earliest timestamp
    /// </summary>
    /// <param name="exchangeBet">The new exchange bet to match</param>
    /// <returns>Match result with details of matches made</returns>
    Task<MatchResult> MatchBetAsync(ExchangeBet exchangeBet);

    /// <summary>
    /// Get all unmatched bets for an outcome
    /// </summary>
    /// <param name="outcomeId">The outcome to get bets for</param>
    /// <param name="side">Optional: filter by side (Back or Lay)</param>
    /// <param name="limit">Maximum number of bets to return</param>
    /// <returns>List of unmatched or partially matched bets</returns>
    Task<List<ExchangeBet>> GetUnmatchedBetsAsync(
        Guid outcomeId,
        BetSide? side = null,
        int limit = 50);

    /// <summary>
    /// Match a specific unmatched bet (user taking someone's bet)
    /// Creates a counter-bet for the user and matches them
    /// </summary>
    /// <param name="exchangeBetId">The bet to take</param>
    /// <param name="userId">The user taking the bet</param>
    /// <param name="stakeToMatch">Amount to match</param>
    /// <returns>Match result</returns>
    Task<MatchResult> TakeBetAsync(Guid exchangeBetId, Guid userId, decimal stakeToMatch);

    /// <summary>
    /// Cancel an unmatched or partially matched bet
    /// Cannot cancel fully matched bets
    /// </summary>
    /// <param name="exchangeBetId">The bet to cancel</param>
    /// <param name="userId">The user requesting cancellation (must be bet owner)</param>
    Task CancelBetAsync(Guid exchangeBetId, Guid userId);
}

/// <summary>
/// Result of a bet matching operation
/// </summary>
public class MatchResult
{
    /// <summary>
    /// Whether the bet was fully matched
    /// </summary>
    public bool FullyMatched { get; set; }

    /// <summary>
    /// Total amount that was matched
    /// </summary>
    public decimal MatchedAmount { get; set; }

    /// <summary>
    /// Amount still unmatched
    /// </summary>
    public decimal UnmatchedAmount { get; set; }

    /// <summary>
    /// List of matches created
    /// </summary>
    public List<BetMatch> Matches { get; set; } = new();

    /// <summary>
    /// Human-readable message about the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
