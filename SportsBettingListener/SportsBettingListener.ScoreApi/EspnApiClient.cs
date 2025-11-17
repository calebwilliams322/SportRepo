using System.Text.Json;
using Microsoft.Extensions.Logging;
using SportsBettingListener.ScoreApi.Models;

namespace SportsBettingListener.ScoreApi;

/// <summary>
/// Client for fetching scores from ESPN's public API (FREE)
/// Endpoints: http://site.api.espn.com/apis/site/v2/sports/{sport}/{league}/scoreboard
/// </summary>
public class EspnApiClient : IScoreApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EspnApiClient> _logger;

    private static readonly Dictionary<string, (string Sport, string League)> SportMappings = new()
    {
        { "americanfootball_nfl", ("football", "nfl") },
        { "basketball_nba", ("basketball", "nba") },
        { "icehockey_nhl", ("hockey", "nhl") },
        { "baseball_mlb", ("baseball", "mlb") }
    };

    public EspnApiClient(HttpClient httpClient, ILogger<EspnApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.BaseAddress = new Uri("http://site.api.espn.com/");
    }

    public async Task<List<ScoreEvent>> FetchScoresAsync(string sport, CancellationToken cancellationToken = default)
    {
        if (!SportMappings.TryGetValue(sport, out var mapping))
        {
            _logger.LogWarning("Sport {Sport} not supported by ESPN API", sport);
            return new List<ScoreEvent>();
        }

        var (espnSport, league) = mapping;
        var url = $"apis/site/v2/sports/{espnSport}/{league}/scoreboard";

        try
        {
            _logger.LogDebug("Fetching scores from ESPN: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var scoreboard = JsonSerializer.Deserialize<EspnScoreboard>(content, options);

            if (scoreboard == null || scoreboard.Events.Count == 0)
            {
                _logger.LogInformation("No events found for {Sport} from ESPN", sport);
                return new List<ScoreEvent>();
            }

            var scoreEvents = scoreboard.Events
                .Select(MapToScoreEvent)
                .ToList();

            _logger.LogInformation("Fetched {Count} events for {Sport} from ESPN",
                scoreEvents.Count, sport);

            return scoreEvents;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching scores from ESPN for {Sport}", sport);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for ESPN scores ({Sport})", sport);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching scores from ESPN for {Sport}", sport);
            throw;
        }
    }

    public bool SupportsSport(string sport)
    {
        return SportMappings.ContainsKey(sport);
    }

    /// <summary>
    /// Maps ESPN event to our internal ScoreEvent model
    /// </summary>
    private ScoreEvent MapToScoreEvent(EspnEvent espnEvent)
    {
        var competition = espnEvent.Competitions.FirstOrDefault();
        if (competition == null)
        {
            _logger.LogWarning("Event {EventId} has no competition data", espnEvent.Id);
            return CreateEmptyScoreEvent(espnEvent);
        }

        var homeCompetitor = competition.Competitors
            .FirstOrDefault(c => c.HomeAway.Equals("home", StringComparison.OrdinalIgnoreCase));
        var awayCompetitor = competition.Competitors
            .FirstOrDefault(c => c.HomeAway.Equals("away", StringComparison.OrdinalIgnoreCase));

        if (homeCompetitor == null || awayCompetitor == null)
        {
            _logger.LogWarning("Event {EventId} missing home or away competitor", espnEvent.Id);
            return CreateEmptyScoreEvent(espnEvent);
        }

        return new ScoreEvent
        {
            ExternalId = espnEvent.Id,
            HomeTeamName = homeCompetitor.Team.DisplayName,
            AwayTeamName = awayCompetitor.Team.DisplayName,
            HomeScore = TryParseScore(homeCompetitor.Score),
            AwayScore = TryParseScore(awayCompetitor.Score),
            EventDate = espnEvent.Date.ToUniversalTime(),
            Status = MapGameStatus(espnEvent.Status),
            Provider = "ESPN"
        };
    }

    /// <summary>
    /// Maps ESPN status to our simplified GameStatus enum
    /// </summary>
    private GameStatus MapGameStatus(EspnStatus espnStatus)
    {
        var statusName = espnStatus.Type.Name.ToUpperInvariant();

        return statusName switch
        {
            "STATUS_SCHEDULED" => GameStatus.Scheduled,
            "STATUS_IN_PROGRESS" => GameStatus.InProgress,
            "STATUS_FINAL" => GameStatus.Final,
            "STATUS_POSTPONED" => GameStatus.Postponed,
            "STATUS_CANCELED" or "STATUS_CANCELLED" => GameStatus.Cancelled,
            _ when espnStatus.Type.Completed => GameStatus.Final,
            _ when espnStatus.Type.State.Equals("in", StringComparison.OrdinalIgnoreCase) => GameStatus.InProgress,
            _ => GameStatus.Scheduled
        };
    }

    /// <summary>
    /// Safely parse score string to int
    /// </summary>
    private int? TryParseScore(string scoreString)
    {
        if (string.IsNullOrWhiteSpace(scoreString))
            return null;

        return int.TryParse(scoreString, out var score) ? score : null;
    }

    /// <summary>
    /// Creates empty score event when data is incomplete
    /// </summary>
    private ScoreEvent CreateEmptyScoreEvent(EspnEvent espnEvent)
    {
        return new ScoreEvent
        {
            ExternalId = espnEvent.Id,
            HomeTeamName = espnEvent.Name,
            AwayTeamName = string.Empty,
            EventDate = espnEvent.Date.ToUniversalTime(),
            Status = MapGameStatus(espnEvent.Status),
            Provider = "ESPN"
        };
    }
}
