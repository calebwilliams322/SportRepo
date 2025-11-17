using System.Text.Json.Serialization;

namespace SportsBettingListener.ScoreApi.Models;

/// <summary>
/// Competition/matchup within an event
/// </summary>
public class EspnCompetition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("competitors")]
    public List<EspnCompetitor> Competitors { get; set; } = new();
}

/// <summary>
/// Team/competitor in the competition
/// </summary>
public class EspnCompetitor
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("homeAway")]
    public string HomeAway { get; set; } = string.Empty;

    [JsonPropertyName("winner")]
    public bool? Winner { get; set; }

    [JsonPropertyName("score")]
    public string Score { get; set; } = "0";

    [JsonPropertyName("team")]
    public EspnTeam Team { get; set; } = new();
}

/// <summary>
/// Team information
/// </summary>
public class EspnTeam
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; } = string.Empty;
}
