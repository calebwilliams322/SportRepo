namespace SportsBetting.Domain.Enums;

/// <summary>
/// Types of bets that can be placed
/// </summary>
public enum BetType
{
    /// <summary>
    /// Single selection bet
    /// </summary>
    Single,

    /// <summary>
    /// Multiple selections, all must win (parlay/accumulator)
    /// </summary>
    Parlay,

    /// <summary>
    /// System bet with multiple combinations
    /// </summary>
    System,

    /// <summary>
    /// Round robin bet (all combinations of parlays)
    /// </summary>
    RoundRobin
}
