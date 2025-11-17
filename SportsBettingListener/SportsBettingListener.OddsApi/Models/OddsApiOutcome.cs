using System.Text.Json.Serialization;

namespace SportsBettingListener.OddsApi.Models;

/// <summary>
/// Represents an outcome (betting option) from The Odds API.
/// Examples: "Kansas City Chiefs", "Over 48.5", etc.
/// </summary>
public class OddsApiOutcome
{
    /// <summary>
    /// Name of the outcome (team name, "Over", "Under", etc.)
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Odds for this outcome (decimal or American format depending on request)
    /// </summary>
    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    /// <summary>
    /// Point value for spreads and totals (e.g., -7.5, 48.5)
    /// Null for moneyline bets
    /// </summary>
    [JsonPropertyName("point")]
    public decimal? Point { get; set; }
}
