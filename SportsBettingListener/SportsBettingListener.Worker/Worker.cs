using SportsBettingListener.OddsApi;
using SportsBettingListener.Sync;

namespace SportsBettingListener.Worker;

/// <summary>
/// Background service that continuously syncs sports events from The Odds API.
/// Runs on a configurable interval and syncs configured sports.
/// </summary>
public class Worker : BackgroundService
{
    private readonly IOddsApiClient _oddsApiClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(
        IOddsApiClient oddsApiClient,
        IServiceProvider serviceProvider,
        ILogger<Worker> logger,
        IConfiguration configuration)
    {
        _oddsApiClient = oddsApiClient ?? throw new ArgumentNullException(nameof(oddsApiClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SportsBettingListener started at: {Time}", DateTimeOffset.Now);

        // Get configuration
        var sports = _configuration.GetSection("OddsApi:Sports").Get<List<string>>()
            ?? new List<string> { "americanfootball_nfl", "basketball_nba" };

        var updateIntervalMinutes = _configuration.GetValue<int>("OddsApi:UpdateIntervalMinutes", 5);
        var updateInterval = TimeSpan.FromMinutes(updateIntervalMinutes);

        _logger.LogInformation("Configured to sync {Count} sports every {Interval} minutes",
            sports.Count, updateIntervalMinutes);

        foreach (var sport in sports)
        {
            _logger.LogInformation("  - {Sport}", sport);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllSportsAsync(sports, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync cycle");
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Next sync in {Minutes} minutes", updateInterval.TotalMinutes);
                await Task.Delay(updateInterval, stoppingToken);
            }
        }

        _logger.LogInformation("SportsBettingListener stopped at: {Time}", DateTimeOffset.Now);
    }

    /// <summary>
    /// Syncs all configured sports in a single cycle.
    /// </summary>
    private async Task SyncAllSportsAsync(List<string> sports, CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.Now;
        _logger.LogInformation("=== Starting sync cycle at {Time} ===", startTime);

        var totalEvents = 0;
        var totalErrors = 0;

        // Step 1: Sync odds from The Odds API
        foreach (var sport in sports)
        {
            try
            {
                var (eventCount, errorCount) = await SyncSportAsync(sport, cancellationToken);
                totalEvents += eventCount;
                totalErrors += errorCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync sport {Sport}", sport);
                totalErrors++;
            }
        }

        // Step 2: Check scores and auto-settle completed games
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var scoreSyncService = scope.ServiceProvider.GetRequiredService<ScoreSyncService>();
                await scoreSyncService.CheckAndSettleCompletedGamesAsync(sports, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check and settle scores");
            totalErrors++;
        }

        var duration = DateTimeOffset.Now - startTime;
        _logger.LogInformation("=== Sync cycle completed in {Duration:F2} seconds: {Events} events, {Errors} errors ===",
            duration.TotalSeconds, totalEvents, totalErrors);
    }

    /// <summary>
    /// Syncs a single sport.
    /// </summary>
    private async Task<(int eventCount, int errorCount)> SyncSportAsync(string sport, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Syncing sport: {Sport}", sport);

        // Fetch events from The Odds API
        var events = await _oddsApiClient.FetchEventsAsync(sport, cancellationToken);

        _logger.LogInformation("Fetched {Count} events for {Sport}", events.Count, sport);

        if (events.Count == 0)
        {
            _logger.LogWarning("No events found for {Sport}", sport);
            return (0, 0);
        }

        var successCount = 0;
        var errorCount = 0;

        // Create a scope for the EventSyncService (it's scoped, not singleton)
        using (var scope = _serviceProvider.CreateScope())
        {
            var syncService = scope.ServiceProvider.GetRequiredService<EventSyncService>();

            // Sync each event
            foreach (var oddsEvent in events)
            {
                try
                {
                    await syncService.SyncEventAsync(oddsEvent, cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to sync event {ExternalId}: {Home} vs {Away}",
                        oddsEvent.Id, oddsEvent.HomeTeam, oddsEvent.AwayTeam);
                    errorCount++;
                }
            }
        }

        _logger.LogInformation("Completed {Sport}: {Success} succeeded, {Failed} failed",
            sport, successCount, errorCount);

        return (successCount, errorCount);
    }
}
