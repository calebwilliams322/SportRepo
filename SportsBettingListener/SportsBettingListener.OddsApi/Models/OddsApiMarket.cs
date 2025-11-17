using System.Text.Json.Serialization;

namespace SportsBettingListener.OddsApi.Models;

/// <summary>
/// Represents a betting market from The Odds API.
/// Markets include: h2h (moneyline), spreads, totals, outrights, etc.
/// </summary>
public class OddsApiMarket
{
    /// <summary>
    /// Market type key: "h2h", "spreads", "totals", "outrights", etc.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of when these odds were last updated
    /// </summary>
    [JsonPropertyName("last_update")]
    public DateTime LastUpdate { get; set; }

    /// <summary>
    /// List of outcomes (betting options) for this market
    /// </summary>
    [JsonPropertyName("outcomes")]
    public List<OddsApiOutcome> Outcomes { get; set; } = new();
}
