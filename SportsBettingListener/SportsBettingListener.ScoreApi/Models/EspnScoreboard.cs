using System.Text.Json.Serialization;

namespace SportsBettingListener.ScoreApi.Models;

/// <summary>
/// Root response from ESPN Scoreboard API
/// </summary>
public class EspnScoreboard
{
    [JsonPropertyName("events")]
    public List<EspnEvent> Events { get; set; } = new();

    [JsonPropertyName("leagues")]
    public List<EspnLeague> Leagues { get; set; } = new();
}

/// <summary>
/// League information from ESPN
/// </summary>
public class EspnLeague
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; } = string.Empty;
}
