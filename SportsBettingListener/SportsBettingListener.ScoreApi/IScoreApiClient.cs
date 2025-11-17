using SportsBettingListener.ScoreApi.Models;

namespace SportsBettingListener.ScoreApi;

/// <summary>
/// Interface for fetching sports scores and results from external providers
/// </summary>
public interface IScoreApiClient
{
    /// <summary>
    /// Fetch current scores for a specific sport
    /// </summary>
    /// <param name="sport">Sport key (e.g., "americanfootball_nfl", "basketball_nba")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of scored events with current status</returns>
    Task<List<ScoreEvent>> FetchScoresAsync(string sport, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if scores are available for a given sport
    /// </summary>
    /// <param name="sport">Sport key</param>
    /// <returns>True if the provider supports this sport</returns>
    bool SupportsSport(string sport);
}
