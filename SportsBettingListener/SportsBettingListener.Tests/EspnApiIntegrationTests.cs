using FluentAssertions;
using Microsoft.Extensions.Logging;
using SportsBettingListener.ScoreApi;
using Xunit;
using Xunit.Abstractions;

namespace SportsBettingListener.Tests;

/// <summary>
/// Integration tests for ESPN API (calls real ESPN endpoints)
/// These tests require internet connection and ESPN API to be available
/// </summary>
public class EspnApiIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<EspnApiClient> _logger;

    public EspnApiIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new XunitLoggerProvider(output));
        });
        _logger = loggerFactory.CreateLogger<EspnApiClient>();
    }

    [Fact]
    public async Task EspnApiClient_ShouldFetchRealNflScores()
    {
        // Arrange
        var httpClient = new HttpClient();
        var client = new EspnApiClient(httpClient, _logger);

        // Act
        var scores = await client.FetchScoresAsync("americanfootball_nfl");

        // Assert
        _output.WriteLine($"Fetched {scores.Count} NFL events from ESPN");

        foreach (var score in scores.Take(5))
        {
            _output.WriteLine($"\n{score.HomeTeamName} vs {score.AwayTeamName}");
            _output.WriteLine($"  Score: {score.HomeScore ?? 0} - {score.AwayScore ?? 0}");
            _output.WriteLine($"  Status: {score.Status}");
            _output.WriteLine($"  ESPN ID: {score.ExternalId}");
            _output.WriteLine($"  Date: {score.EventDate:yyyy-MM-dd HH:mm} UTC");
            _output.WriteLine($"  Is Completed: {score.IsCompleted}");
            _output.WriteLine($"  Is Live: {score.IsLive}");

            // Verify data structure
            score.ExternalId.Should().NotBeNullOrEmpty("ESPN should provide event IDs");
            score.HomeTeamName.Should().NotBeNullOrEmpty("Home team name should be present");
            score.AwayTeamName.Should().NotBeNullOrEmpty("Away team name should be present");
            score.Provider.Should().Be("ESPN");
            score.EventDate.Should().BeAfter(DateTime.UtcNow.AddYears(-1), "Event date should be recent");
        }

        // Should return some events (might be 0 in off-season)
        scores.Should().NotBeNull();
    }

    [Fact]
    public async Task EspnApiClient_ShouldFetchRealNbaScores()
    {
        // Arrange
        var httpClient = new HttpClient();
        var client = new EspnApiClient(httpClient, _logger);

        // Act
        var scores = await client.FetchScoresAsync("basketball_nba");

        // Assert
        _output.WriteLine($"Fetched {scores.Count} NBA events from ESPN");

        foreach (var score in scores.Take(3))
        {
            _output.WriteLine($"\n{score.HomeTeamName} vs {score.AwayTeamName}");
            _output.WriteLine($"  Score: {score.HomeScore ?? 0} - {score.AwayScore ?? 0}");
            _output.WriteLine($"  Status: {score.Status}");
            _output.WriteLine($"  ESPN ID: {score.ExternalId}");
        }

        scores.Should().NotBeNull();
    }

    [Fact]
    public void EspnApiClient_ShouldSupportMajorSports()
    {
        // Arrange
        var httpClient = new HttpClient();
        var client = new EspnApiClient(httpClient, _logger);

        // Assert
        client.SupportsSport("americanfootball_nfl").Should().BeTrue();
        client.SupportsSport("basketball_nba").Should().BeTrue();
        client.SupportsSport("icehockey_nhl").Should().BeTrue();
        client.SupportsSport("baseball_mlb").Should().BeTrue();

        // Should not support unsupported sports
        client.SupportsSport("soccer_epl").Should().BeFalse();
        client.SupportsSport("invalid_sport").Should().BeFalse();
    }
}

/// <summary>
/// Simple Xunit logger provider for tests
/// </summary>
public class XunitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XunitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(_output, categoryName);
    }

    public void Dispose() { }
}

public class XunitLogger : ILogger
{
    private readonly ITestOutputHelper _output;
    private readonly string _categoryName;

    public XunitLogger(ITestOutputHelper output, string categoryName)
    {
        _output = output;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) => null!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _output.WriteLine($"[{logLevel}] {_categoryName}: {formatter(state, exception)}");
        if (exception != null)
            _output.WriteLine(exception.ToString());
    }
}
