namespace SportsBetting.Domain.Enums;

/// <summary>
/// State of a bet (both sportsbook and exchange)
/// </summary>
public enum BetState
{
    // Sportsbook states
    /// <summary>
    /// Sportsbook: Bet placed, awaiting event result
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Sportsbook: Bet won
    /// </summary>
    Won = 1,

    /// <summary>
    /// Sportsbook: Bet lost
    /// </summary>
    Lost = 2,

    /// <summary>
    /// Sportsbook: Bet pushed (tie/refund)
    /// </summary>
    Pushed = 3,

    /// <summary>
    /// Sportsbook: Bet settled
    /// </summary>
    Settled = 4,

    // Exchange states
    /// <summary>
    /// Exchange: Bet placed but not yet matched with opposing bet
    /// </summary>
    Unmatched = 10,

    /// <summary>
    /// Exchange: Part of stake matched, part still unmatched
    /// </summary>
    PartiallyMatched = 11,

    /// <summary>
    /// Exchange: Fully matched with opposing bet(s)
    /// </summary>
    Matched = 12,

    /// <summary>
    /// Exchange: Cancelled by user before matching
    /// </summary>
    Cancelled = 13
}
