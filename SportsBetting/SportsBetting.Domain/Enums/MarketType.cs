namespace SportsBetting.Domain.Enums;

/// <summary>
/// Types of betting markets available
/// </summary>
public enum MarketType
{
    /// <summary>
    /// Straight bet on which team/participant wins
    /// </summary>
    Moneyline,

    /// <summary>
    /// Bet on team to cover a point spread/handicap
    /// </summary>
    Spread,

    /// <summary>
    /// Bet on total combined score over/under a line
    /// </summary>
    Totals,

    /// <summary>
    /// Proposition bet (player props, game props, etc.)
    /// </summary>
    Prop,

    /// <summary>
    /// Future bet (championship winner, season totals, etc.)
    /// </summary>
    Futures
}
