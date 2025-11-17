namespace SportsBetting.Domain.Enums;

/// <summary>
/// Market mode: determines what type of betting is available
/// </summary>
public enum MarketMode
{
    /// <summary>
    /// Traditional sportsbook only - house sets odds
    /// </summary>
    Sportsbook = 0,

    /// <summary>
    /// Exchange only - users propose odds and match with each other
    /// </summary>
    Exchange = 1,

    /// <summary>
    /// Hybrid - both sportsbook and exchange available for this market
    /// </summary>
    Hybrid = 2
}
