namespace SportsBetting.Domain.Enums;

/// <summary>
/// Represents the current state of a LineLock
/// </summary>
public enum LineLockStatus
{
    /// <summary>
    /// Lock is active and can be exercised
    /// </summary>
    Active,

    /// <summary>
    /// Lock was exercised and converted to a bet
    /// </summary>
    Used,

    /// <summary>
    /// Lock expired without being exercised
    /// </summary>
    Expired,

    /// <summary>
    /// Lock was cancelled (e.g., event cancelled) - fee refunded
    /// </summary>
    Cancelled
}
