using System.Text.Json.Serialization;

namespace SportsBettingListener.OddsApi.Models;

/// <summary>
/// Represents a bookmaker/sportsbook from The Odds API.
/// Examples: DraftKings, FanDuel, BetMGM, Caesars, etc.
/// </summary>
public class OddsApiBookmaker
{
    /// <summary>
    /// Bookmaker identifier key (e.g., "draftkings", "fanduel")
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable bookmaker name (e.g., "DraftKings", "FanDuel")
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of when this bookmaker's odds were last updated
    /// </summary>
    [JsonPropertyName("last_update")]
    public DateTime LastUpdate { get; set; }

    /// <summary>
    /// List of betting markets offered by this bookmaker for this event
    /// </summary>
    [JsonPropertyName("markets")]
    public List<OddsApiMarket> Markets { get; set; } = new();
}
