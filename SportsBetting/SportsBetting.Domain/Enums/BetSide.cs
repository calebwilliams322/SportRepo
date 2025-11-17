namespace SportsBetting.Domain.Enums;

/// <summary>
/// Side of an exchange bet
/// </summary>
public enum BetSide
{
    /// <summary>
    /// Betting FOR an outcome (traditional betting)
    /// </summary>
    Back = 0,

    /// <summary>
    /// Betting AGAINST an outcome (acting as the bookmaker)
    /// </summary>
    Lay = 1
}
