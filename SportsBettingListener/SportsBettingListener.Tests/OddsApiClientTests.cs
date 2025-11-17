using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SportsBettingListener.OddsApi;
using SportsBettingListener.OddsApi.Models;
using Xunit;

namespace SportsBettingListener.Tests;

/// <summary>
/// Tests for The Odds API client functionality.
/// These tests verify JSON deserialization and client initialization.
/// </summary>
public class OddsApiClientTests
{
    [Fact]
    public void OddsApiEvent_ShouldDeserializeFromJson()
    {
        // Arrange - Sample JSON response from The Odds API documentation
        var json = @"
        {
            ""id"": ""test123"",
            ""sport_key"": ""americanfootball_nfl"",
            ""sport_title"": ""NFL"",
            ""commence_time"": ""2025-11-20T18:00:00Z"",
            ""home_team"": ""Kansas City Chiefs"",
            ""away_team"": ""Las Vegas Raiders"",
            ""bookmakers"": [
                {
                    ""key"": ""draftkings"",
                    ""title"": ""DraftKings"",
                    ""last_update"": ""2025-11-15T12:30:00Z"",
                    ""markets"": [
                        {
                            ""key"": ""h2h"",
                            ""last_update"": ""2025-11-15T12:30:00Z"",
                            ""outcomes"": [
                                {
                                    ""name"": ""Kansas City Chiefs"",
                                    ""price"": 1.45
                                },
                                {
                                    ""name"": ""Las Vegas Raiders"",
                                    ""price"": 2.90
                                }
                            ]
                        },
                        {
                            ""key"": ""spreads"",
                            ""last_update"": ""2025-11-15T12:30:00Z"",
                            ""outcomes"": [
                                {
                                    ""name"": ""Kansas City Chiefs"",
                                    ""price"": 1.91,
                                    ""point"": -7.5
                                },
                                {
                                    ""name"": ""Las Vegas Raiders"",
                                    ""price"": 1.91,
                                    ""point"": 7.5
                                }
                            ]
                        }
                    ]
                }
            ]
        }";

        // Act
        var oddsEvent = JsonSerializer.Deserialize<OddsApiEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        oddsEvent.Should().NotBeNull();
        oddsEvent!.Id.Should().Be("test123");
        oddsEvent.SportKey.Should().Be("americanfootball_nfl");
        oddsEvent.SportTitle.Should().Be("NFL");
        oddsEvent.HomeTeam.Should().Be("Kansas City Chiefs");
        oddsEvent.AwayTeam.Should().Be("Las Vegas Raiders");
        oddsEvent.Bookmakers.Should().HaveCount(1);

        var bookmaker = oddsEvent.Bookmakers[0];
        bookmaker.Key.Should().Be("draftkings");
        bookmaker.Title.Should().Be("DraftKings");
        bookmaker.Markets.Should().HaveCount(2);

        var moneylineMarket = bookmaker.Markets[0];
        moneylineMarket.Key.Should().Be("h2h");
        moneylineMarket.Outcomes.Should().HaveCount(2);
        moneylineMarket.Outcomes[0].Name.Should().Be("Kansas City Chiefs");
        moneylineMarket.Outcomes[0].Price.Should().Be(1.45m);

        var spreadMarket = bookmaker.Markets[1];
        spreadMarket.Key.Should().Be("spreads");
        spreadMarket.Outcomes[0].Point.Should().Be(-7.5m);
        spreadMarket.Outcomes[1].Point.Should().Be(7.5m);
    }

    [Fact]
    public void OddsApiClient_ShouldInitialize_WithValidConfiguration()
    {
        // Arrange
        var mockHttpClient = new HttpClient();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<OddsApiClient>>();

        // Setup configuration to return a test API key
        mockConfiguration.Setup(c => c["OddsApi:ApiKey"]).Returns("test-api-key");

        // Act
        var client = new OddsApiClient(mockHttpClient, mockConfiguration.Object, mockLogger.Object);

        // Assert
        client.Should().NotBeNull();
        client.Should().BeAssignableTo<IOddsApiClient>();
    }

    [Fact]
    public void OddsApiClient_ShouldThrow_WhenApiKeyMissing()
    {
        // Arrange
        var mockHttpClient = new HttpClient();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockLogger = new Mock<ILogger<OddsApiClient>>();

        // Setup configuration to return null (missing API key)
        mockConfiguration.Setup(c => c["OddsApi:ApiKey"]).Returns((string?)null);

        // Act & Assert
        Action act = () => new OddsApiClient(mockHttpClient, mockConfiguration.Object, mockLogger.Object);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*OddsApi:ApiKey*");
    }

    [Fact]
    public void OddsApiEvent_ShouldHandleMultipleBookmakers()
    {
        // Arrange - Test with multiple bookmakers
        var json = @"
        {
            ""id"": ""test456"",
            ""sport_key"": ""basketball_nba"",
            ""sport_title"": ""NBA"",
            ""commence_time"": ""2025-11-21T20:00:00Z"",
            ""home_team"": ""Los Angeles Lakers"",
            ""away_team"": ""Boston Celtics"",
            ""bookmakers"": [
                {
                    ""key"": ""draftkings"",
                    ""title"": ""DraftKings"",
                    ""last_update"": ""2025-11-15T14:00:00Z"",
                    ""markets"": [
                        {
                            ""key"": ""h2h"",
                            ""last_update"": ""2025-11-15T14:00:00Z"",
                            ""outcomes"": [
                                { ""name"": ""Los Angeles Lakers"", ""price"": 1.85 },
                                { ""name"": ""Boston Celtics"", ""price"": 2.05 }
                            ]
                        }
                    ]
                },
                {
                    ""key"": ""fanduel"",
                    ""title"": ""FanDuel"",
                    ""last_update"": ""2025-11-15T14:01:00Z"",
                    ""markets"": [
                        {
                            ""key"": ""h2h"",
                            ""last_update"": ""2025-11-15T14:01:00Z"",
                            ""outcomes"": [
                                { ""name"": ""Los Angeles Lakers"", ""price"": 1.87 },
                                { ""name"": ""Boston Celtics"", ""price"": 2.03 }
                            ]
                        }
                    ]
                }
            ]
        }";

        // Act
        var oddsEvent = JsonSerializer.Deserialize<OddsApiEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        oddsEvent.Should().NotBeNull();
        oddsEvent!.Bookmakers.Should().HaveCount(2);
        oddsEvent.Bookmakers[0].Key.Should().Be("draftkings");
        oddsEvent.Bookmakers[1].Key.Should().Be("fanduel");

        // Verify different odds from different bookmakers
        oddsEvent.Bookmakers[0].Markets[0].Outcomes[0].Price.Should().Be(1.85m);
        oddsEvent.Bookmakers[1].Markets[0].Outcomes[0].Price.Should().Be(1.87m);
    }

    [Fact]
    public void OddsApiEvent_ShouldHandleEmptyBookmakers()
    {
        // Arrange - Event with no bookmakers (rare but possible)
        var json = @"
        {
            ""id"": ""test789"",
            ""sport_key"": ""soccer_epl"",
            ""sport_title"": ""EPL"",
            ""commence_time"": ""2025-11-22T15:00:00Z"",
            ""home_team"": ""Manchester United"",
            ""away_team"": ""Liverpool"",
            ""bookmakers"": []
        }";

        // Act
        var oddsEvent = JsonSerializer.Deserialize<OddsApiEvent>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        oddsEvent.Should().NotBeNull();
        oddsEvent!.Bookmakers.Should().BeEmpty();
    }

    [Fact]
    public void OddsApiOutcome_ShouldHandleNullPoint()
    {
        // Arrange - Moneyline outcome without point value
        var json = @"
        {
            ""name"": ""Dallas Cowboys"",
            ""price"": 1.65
        }";

        // Act
        var outcome = JsonSerializer.Deserialize<OddsApiOutcome>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        outcome.Should().NotBeNull();
        outcome!.Name.Should().Be("Dallas Cowboys");
        outcome.Price.Should().Be(1.65m);
        outcome.Point.Should().BeNull();
    }
}
