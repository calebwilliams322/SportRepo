namespace SportsBetting.Domain.Enums;

/// <summary>
/// Represents whether a user provided or took liquidity in a trade
/// Used for liquidity provider incentives
/// </summary>
public enum LiquidityRole
{
    /// <summary>
    /// Maker - Provided liquidity by placing an order that sat in the order book
    /// Receives discount on commission (e.g., -20% off base rate)
    /// </summary>
    Maker = 0,

    /// <summary>
    /// Taker - Took liquidity by matching against an existing order
    /// Pays standard commission rate
    /// </summary>
    Taker = 1
}
