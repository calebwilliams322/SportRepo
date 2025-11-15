namespace SportsBetting.Domain.Enums;

/// <summary>
/// Represents the current state of a bet
/// </summary>
public enum BetStatus
{
    /// <summary>
    /// Bet has been placed and is waiting for event to complete
    /// </summary>
    Pending,

    /// <summary>
    /// Bet acceptance is pending (e.g., odds verification)
    /// </summary>
    PendingAcceptance,

    /// <summary>
    /// Bet has been settled as a win
    /// </summary>
    Won,

    /// <summary>
    /// Bet has been settled as a loss
    /// </summary>
    Lost,

    /// <summary>
    /// Bet resulted in a push (tie) - stake returned
    /// </summary>
    Pushed,

    /// <summary>
    /// Bet was cancelled or voided
    /// </summary>
    Void
}
