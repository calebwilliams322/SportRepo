using System.Text.Json.Serialization;

namespace SportsBettingListener.OddsApi.Models;

/// <summary>
/// Represents a sports event (game/match) from The Odds API.
/// This is the top-level object returned when fetching odds.
/// </summary>
public class OddsApiEvent
{
    /// <summary>
    /// Unique event identifier from The Odds API
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Sport key identifier (e.g., "americanfootball_nfl", "basketball_nba")
    /// </summary>
    [JsonPropertyName("sport_key")]
    public string SportKey { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable sport title (e.g., "NFL", "NBA")
    /// </summary>
    [JsonPropertyName("sport_title")]
    public string SportTitle { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled start time of the event (UTC)
    /// </summary>
    [JsonPropertyName("commence_time")]
    public DateTime CommenceTime { get; set; }

    /// <summary>
    /// Home team name
    /// </summary>
    [JsonPropertyName("home_team")]
    public string HomeTeam { get; set; } = string.Empty;

    /// <summary>
    /// Away team name
    /// </summary>
    [JsonPropertyName("away_team")]
    public string AwayTeam { get; set; } = string.Empty;

    /// <summary>
    /// List of bookmakers offering odds for this event
    /// Each bookmaker contains markets with odds
    /// </summary>
    [JsonPropertyName("bookmakers")]
    public List<OddsApiBookmaker> Bookmakers { get; set; } = new();
}
