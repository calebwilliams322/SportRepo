namespace SportsBetting.Domain.Enums;

/// <summary>
/// Commission tier based on 30-day trading volume
/// Lower tiers = higher commission, Higher tiers = lower commission
/// </summary>
public enum CommissionTier
{
    /// <summary>
    /// Standard tier - Default for new users
    /// 30-day volume: $0 - $10,000
    /// Base commission: 5%
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Bronze tier
    /// 30-day volume: $10,000 - $50,000
    /// Base commission: 4%
    /// </summary>
    Bronze = 1,

    /// <summary>
    /// Silver tier
    /// 30-day volume: $50,000 - $200,000
    /// Base commission: 3%
    /// </summary>
    Silver = 2,

    /// <summary>
    /// Gold tier
    /// 30-day volume: $200,000 - $1,000,000
    /// Base commission: 2%
    /// </summary>
    Gold = 3,

    /// <summary>
    /// Platinum tier - VIP users
    /// 30-day volume: $1,000,000+
    /// Base commission: 1%
    /// </summary>
    Platinum = 4
}
