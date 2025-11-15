namespace SportsBetting.Domain.Enums;

/// <summary>
/// Result of an individual bet selection after settlement
/// </summary>
public enum SelectionResult
{
    /// <summary>
    /// Selection has not been settled yet
    /// </summary>
    Pending,

    /// <summary>
    /// Selection won
    /// </summary>
    Won,

    /// <summary>
    /// Selection lost
    /// </summary>
    Lost,

    /// <summary>
    /// Selection pushed (tie on the line)
    /// </summary>
    Pushed,

    /// <summary>
    /// Selection was voided
    /// </summary>
    Void
}
