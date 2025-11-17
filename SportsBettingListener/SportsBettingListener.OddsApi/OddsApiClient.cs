using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SportsBettingListener.OddsApi.Models;

namespace SportsBettingListener.OddsApi;

/// <summary>
/// Client for interacting with The Odds API (https://the-odds-api.com).
/// Fetches live sports events, odds, and betting markets from various bookmakers.
/// </summary>
public class OddsApiClient : IOddsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<OddsApiClient> _logger;
    private const string BaseUrl = "https://api.the-odds-api.com/v4/";

    public OddsApiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OddsApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Get API key from configuration
        _apiKey = configuration["OddsApi:ApiKey"]
            ?? throw new InvalidOperationException("OddsApi:ApiKey is not configured in appsettings.json");

        // Set base address
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    /// <inheritdoc/>
    public async Task<List<OddsApiEvent>> FetchEventsAsync(
        string sport,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build the URL with query parameters
            // Request decimal odds format for easier processing
            // Include h2h (moneyline), spreads, and totals markets
            var url = $"sports/{sport}/odds" +
                      $"?apiKey={_apiKey}" +
                      $"&regions=us" +
                      $"&markets=h2h,spreads,totals" +
                      $"&oddsFormat=decimal";

            _logger.LogInformation("Fetching odds for sport: {Sport}", sport);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            // Log rate limit information from response headers
            LogRateLimitInfo(response);

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var events = JsonSerializer.Deserialize<List<OddsApiEvent>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<OddsApiEvent>();

            _logger.LogInformation("Successfully fetched {Count} events for {Sport}", events.Count, sport);

            return events;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching events for sport {Sport}", sport);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for sport {Sport}", sport);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching events for sport {Sport}", sport);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetAvailableSportsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"sports?apiKey={_apiKey}";

            _logger.LogInformation("Fetching available sports list");

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            // The API returns an array of sport objects with "key" property
            var sports = JsonSerializer.Deserialize<List<SportInfo>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<SportInfo>();

            var sportKeys = sports
                .Where(s => s.Active) // Only return active sports
                .Select(s => s.Key)
                .ToList();

            _logger.LogInformation("Found {Count} active sports", sportKeys.Count);

            return sportKeys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available sports");
            throw;
        }
    }

    /// <summary>
    /// Logs rate limit information from The Odds API response headers.
    /// Helps monitor API quota usage.
    /// </summary>
    private void LogRateLimitInfo(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("x-requests-remaining", out var remainingValues))
        {
            var remaining = remainingValues.FirstOrDefault();
            _logger.LogInformation("API requests remaining: {Remaining}", remaining);

            // Warn if running low on quota
            if (int.TryParse(remaining, out var remainingInt) && remainingInt < 100)
            {
                _logger.LogWarning("Running low on API quota! Only {Remaining} requests remaining", remaining);
            }
        }

        if (response.Headers.TryGetValues("x-requests-used", out var usedValues))
        {
            var used = usedValues.FirstOrDefault();
            _logger.LogDebug("API requests used this month: {Used}", used);
        }
    }

    /// <summary>
    /// Internal class for deserializing the sports list response.
    /// </summary>
    private class SportInfo
    {
        public string Key { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
