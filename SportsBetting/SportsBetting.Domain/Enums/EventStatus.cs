namespace SportsBetting.Domain.Enums;

/// <summary>
/// Represents the current state of a sporting event
/// </summary>
public enum EventStatus
{
    /// <summary>
    /// Event is scheduled but hasn't started yet
    /// </summary>
    Scheduled,

    /// <summary>
    /// Event is currently in progress (in-play)
    /// </summary>
    InProgress,

    /// <summary>
    /// Event has finished and results are final
    /// </summary>
    Completed,

    /// <summary>
    /// Event has been cancelled
    /// </summary>
    Cancelled,

    /// <summary>
    /// Event is temporarily suspended
    /// </summary>
    Suspended
}
