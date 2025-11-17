using System.Text.Json.Serialization;

namespace SportsBettingListener.ScoreApi.Models;

/// <summary>
/// Game/Event from ESPN API
/// </summary>
public class EspnEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("status")]
    public EspnStatus Status { get; set; } = new();

    [JsonPropertyName("competitions")]
    public List<EspnCompetition> Competitions { get; set; } = new();
}

/// <summary>
/// Event status information
/// </summary>
public class EspnStatus
{
    [JsonPropertyName("type")]
    public EspnStatusType Type { get; set; } = new();
}

/// <summary>
/// Status type details
/// </summary>
public class EspnStatusType
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
