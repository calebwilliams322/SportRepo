namespace SportsBetting.Domain.Enums;

/// <summary>
/// Bet mode: traditional sportsbook or peer-to-peer exchange
/// </summary>
public enum BetMode
{
    /// <summary>
    /// Traditional betting: user bets against the house at fixed odds
    /// </summary>
    Sportsbook = 0,

    /// <summary>
    /// P2P exchange: user bets against other users
    /// </summary>
    Exchange = 1
}
