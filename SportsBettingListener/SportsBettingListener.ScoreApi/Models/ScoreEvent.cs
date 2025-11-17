namespace SportsBettingListener.ScoreApi.Models;

/// <summary>
/// Abstraction of a scored event (provider-agnostic)
/// Maps ESPN, Sportradar, or other providers to a common format
/// </summary>
public class ScoreEvent
{
    public string ExternalId { get; set; } = string.Empty;
    public string HomeTeamName { get; set; } = string.Empty;
    public string AwayTeamName { get; set; } = string.Empty;
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public DateTime EventDate { get; set; }
    public GameStatus Status { get; set; }
    public string Provider { get; set; } = "ESPN";

    /// <summary>
    /// True if the game has finished
    /// </summary>
    public bool IsCompleted => Status == GameStatus.Final;

    /// <summary>
    /// True if the game is currently in progress
    /// </summary>
    public bool IsLive => Status == GameStatus.InProgress;
}

/// <summary>
/// Simplified game status
/// </summary>
public enum GameStatus
{
    Scheduled,
    InProgress,
    Final,
    Postponed,
    Cancelled
}
