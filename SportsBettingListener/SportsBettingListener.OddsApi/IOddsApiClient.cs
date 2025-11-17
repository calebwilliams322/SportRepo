using SportsBettingListener.OddsApi.Models;

namespace SportsBettingListener.OddsApi;

/// <summary>
/// Interface for interacting with The Odds API.
/// Provides methods to fetch sports events, odds, and available sports.
/// </summary>
public interface IOddsApiClient
{
    /// <summary>
    /// Fetches all events with odds for a specific sport.
    /// </summary>
    /// <param name="sport">Sport key (e.g., "americanfootball_nfl", "basketball_nba")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of events with bookmaker odds</returns>
    Task<List<OddsApiEvent>> FetchEventsAsync(string sport, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the list of available sports from The Odds API.
    /// Useful for discovering sport keys.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of sport keys (e.g., ["americanfootball_nfl", "basketball_nba"])</returns>
    Task<List<string>> GetAvailableSportsAsync(CancellationToken cancellationToken = default);
}
