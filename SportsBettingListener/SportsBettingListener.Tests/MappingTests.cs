using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SportsBetting.Data;
using SportsBetting.Domain.Enums;
using SportsBettingListener.OddsApi.Models;
using SportsBettingListener.Sync.Mappers;
using Xunit;

namespace SportsBettingListener.Tests;

/// <summary>
/// Tests for mapping The Odds API data to domain entities.
/// </summary>
public class MappingTests : IDisposable
{
    private readonly SportsBettingDbContext _context;
    private readonly EventMapper _eventMapper;
    private readonly MarketMapper _marketMapper;

    public MappingTests()
    {
        // Setup in-memory database for testing
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SportsBettingDbContext(options);

        // Create mappers with mocked loggers
        var eventMapperLogger = new Mock<ILogger<EventMapper>>();
        var marketMapperLogger = new Mock<ILogger<MarketMapper>>();

        _eventMapper = new EventMapper(_context, eventMapperLogger.Object);
        _marketMapper = new MarketMapper(marketMapperLogger.Object);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task EventMapper_ShouldCreateEventWithTeamsAndLeague()
    {
        // Arrange
        var oddsEvent = new OddsApiEvent
        {
            Id = "test123",
            SportKey = "americanfootball_nfl",
            SportTitle = "NFL",
            CommenceTime = new DateTime(2025, 11, 20, 18, 0, 0, DateTimeKind.Utc),
            HomeTeam = "Kansas City Chiefs",
            AwayTeam = "Las Vegas Raiders",
            Bookmakers = new List<OddsApiBookmaker>()
        };

        // Act
        var evt = await _eventMapper.MapToEventAsync(oddsEvent);

        // Assert
        evt.Should().NotBeNull();
        evt.ExternalId.Should().Be("test123");
        evt.Name.Should().Be("Kansas City Chiefs vs Las Vegas Raiders");
        evt.HomeTeam.Should().NotBeNull();
        evt.AwayTeam.Should().NotBeNull();
        evt.HomeTeam.Name.Should().Be("Chiefs");
        evt.AwayTeam.Name.Should().Be("Raiders");
        evt.ScheduledStartTime.Should().Be(new DateTime(2025, 11, 20, 18, 0, 0, DateTimeKind.Utc));

        // Verify Sport was created
        var sport = await _context.Sports.FirstOrDefaultAsync();
        sport.Should().NotBeNull();
        sport!.Name.Should().Be("NFL");
        sport.Code.Should().Be("AMERICANFOOTBALL_NFL");

        // Verify League was created
        var league = await _context.Leagues.FirstOrDefaultAsync();
        league.Should().NotBeNull();
        league!.Name.Should().Be("NFL");
        league.Code.Should().Be("NFL");

        // Verify Teams were created
        var teams = await _context.Teams.ToListAsync();
        teams.Should().HaveCount(2);
        teams.Should().Contain(t => t.Name == "Chiefs");
        teams.Should().Contain(t => t.Name == "Raiders");
    }

    [Fact]
    public async Task EventMapper_ShouldReuseExistingSportAndLeague()
    {
        // Arrange - First event creates sport and league
        var firstEvent = new OddsApiEvent
        {
            Id = "event1",
            SportKey = "basketball_nba",
            SportTitle = "NBA",
            CommenceTime = DateTime.UtcNow.AddDays(1),
            HomeTeam = "Los Angeles Lakers",
            AwayTeam = "Boston Celtics",
            Bookmakers = new List<OddsApiBookmaker>()
        };

        await _eventMapper.MapToEventAsync(firstEvent);

        var sportCountBefore = await _context.Sports.CountAsync();
        var leagueCountBefore = await _context.Leagues.CountAsync();

        // Act - Second event should reuse the same sport and league
        var secondEvent = new OddsApiEvent
        {
            Id = "event2",
            SportKey = "basketball_nba",
            SportTitle = "NBA",
            CommenceTime = DateTime.UtcNow.AddDays(2),
            HomeTeam = "Miami Heat",
            AwayTeam = "Chicago Bulls",
            Bookmakers = new List<OddsApiBookmaker>()
        };

        await _eventMapper.MapToEventAsync(secondEvent);

        // Assert - Sport and League count should not increase
        var sportCountAfter = await _context.Sports.CountAsync();
        var leagueCountAfter = await _context.Leagues.CountAsync();

        sportCountAfter.Should().Be(sportCountBefore);
        leagueCountAfter.Should().Be(leagueCountBefore);

        // But teams should increase
        var teamCount = await _context.Teams.CountAsync();
        teamCount.Should().Be(4); // 2 from first event + 2 from second event
    }

    [Fact]
    public void MarketMapper_ShouldCreateMoneylineMarket()
    {
        // Arrange
        var oddsEvent = new OddsApiEvent
        {
            Id = "test456",
            SportKey = "americanfootball_nfl",
            SportTitle = "NFL",
            CommenceTime = DateTime.UtcNow.AddDays(1),
            HomeTeam = "Kansas City Chiefs",
            AwayTeam = "Las Vegas Raiders",
            Bookmakers = new List<OddsApiBookmaker>
            {
                new OddsApiBookmaker
                {
                    Key = "draftkings",
                    Title = "DraftKings",
                    LastUpdate = DateTime.UtcNow,
                    Markets = new List<OddsApiMarket>
                    {
                        new OddsApiMarket
                        {
                            Key = "h2h",
                            LastUpdate = DateTime.UtcNow,
                            Outcomes = new List<OddsApiOutcome>
                            {
                                new OddsApiOutcome { Name = "Kansas City Chiefs", Price = 1.45m },
                                new OddsApiOutcome { Name = "Las Vegas Raiders", Price = 2.90m }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var markets = _marketMapper.MapMarketsForEvent(oddsEvent);

        // Assert
        markets.Should().HaveCount(1);
        var market = markets[0];
        market.Type.Should().Be(MarketType.Moneyline);
        market.Name.Should().Be("Moneyline");
        market.ExternalId.Should().Be("h2h");
        market.Outcomes.Should().HaveCount(2);

        var chiefsOutcome = market.Outcomes.First(o => o.Name == "Kansas City Chiefs");
        chiefsOutcome.CurrentOdds.DecimalValue.Should().Be(1.45m);
        chiefsOutcome.Line.Should().BeNull();

        var raidersOutcome = market.Outcomes.First(o => o.Name == "Las Vegas Raiders");
        raidersOutcome.CurrentOdds.DecimalValue.Should().Be(2.90m);
    }

    [Fact]
    public void MarketMapper_ShouldCreateSpreadMarket()
    {
        // Arrange
        var oddsEvent = new OddsApiEvent
        {
            Id = "test789",
            SportKey = "americanfootball_nfl",
            SportTitle = "NFL",
            CommenceTime = DateTime.UtcNow.AddDays(1),
            HomeTeam = "Kansas City Chiefs",
            AwayTeam = "Las Vegas Raiders",
            Bookmakers = new List<OddsApiBookmaker>
            {
                new OddsApiBookmaker
                {
                    Key = "draftkings",
                    Title = "DraftKings",
                    LastUpdate = DateTime.UtcNow,
                    Markets = new List<OddsApiMarket>
                    {
                        new OddsApiMarket
                        {
                            Key = "spreads",
                            LastUpdate = DateTime.UtcNow,
                            Outcomes = new List<OddsApiOutcome>
                            {
                                new OddsApiOutcome { Name = "Kansas City Chiefs", Price = 1.91m, Point = -7.5m },
                                new OddsApiOutcome { Name = "Las Vegas Raiders", Price = 1.91m, Point = 7.5m }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var markets = _marketMapper.MapMarketsForEvent(oddsEvent);

        // Assert
        markets.Should().HaveCount(1);
        var market = markets[0];
        market.Type.Should().Be(MarketType.Spread);
        market.Name.Should().Be("Point Spread");
        market.Outcomes.Should().HaveCount(2);

        var chiefsOutcome = market.Outcomes.First(o => o.Name == "Kansas City Chiefs");
        chiefsOutcome.Line.Should().Be(-7.5m);
        chiefsOutcome.Description.Should().Contain("-7.5");

        var raidersOutcome = market.Outcomes.First(o => o.Name == "Las Vegas Raiders");
        raidersOutcome.Line.Should().Be(7.5m);
        raidersOutcome.Description.Should().Contain("+7.5");
    }

    [Fact]
    public void MarketMapper_ShouldCreateTotalsMarket()
    {
        // Arrange
        var oddsEvent = new OddsApiEvent
        {
            Id = "test101",
            SportKey = "americanfootball_nfl",
            SportTitle = "NFL",
            CommenceTime = DateTime.UtcNow.AddDays(1),
            HomeTeam = "Kansas City Chiefs",
            AwayTeam = "Las Vegas Raiders",
            Bookmakers = new List<OddsApiBookmaker>
            {
                new OddsApiBookmaker
                {
                    Key = "draftkings",
                    Title = "DraftKings",
                    LastUpdate = DateTime.UtcNow,
                    Markets = new List<OddsApiMarket>
                    {
                        new OddsApiMarket
                        {
                            Key = "totals",
                            LastUpdate = DateTime.UtcNow,
                            Outcomes = new List<OddsApiOutcome>
                            {
                                new OddsApiOutcome { Name = "Over", Price = 1.87m, Point = 48.5m },
                                new OddsApiOutcome { Name = "Under", Price = 1.95m, Point = 48.5m }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var markets = _marketMapper.MapMarketsForEvent(oddsEvent);

        // Assert
        markets.Should().HaveCount(1);
        var market = markets[0];
        market.Type.Should().Be(MarketType.Totals);
        market.Name.Should().Be("Total Points");
        market.Description.Should().Contain("48.5");
        market.Outcomes.Should().HaveCount(2);

        var overOutcome = market.Outcomes.First(o => o.Name == "Over");
        overOutcome.Line.Should().Be(48.5m);
        overOutcome.CurrentOdds.DecimalValue.Should().Be(1.87m);

        var underOutcome = market.Outcomes.First(o => o.Name == "Under");
        underOutcome.Line.Should().Be(48.5m);
        underOutcome.CurrentOdds.DecimalValue.Should().Be(1.95m);
    }

    [Fact]
    public void MarketMapper_ShouldCreateAllThreeMarketTypes()
    {
        // Arrange - Event with all three market types
        var oddsEvent = new OddsApiEvent
        {
            Id = "comprehensive-test",
            SportKey = "americanfootball_nfl",
            SportTitle = "NFL",
            CommenceTime = DateTime.UtcNow.AddDays(1),
            HomeTeam = "Dallas Cowboys",
            AwayTeam = "Philadelphia Eagles",
            Bookmakers = new List<OddsApiBookmaker>
            {
                new OddsApiBookmaker
                {
                    Key = "draftkings",
                    Title = "DraftKings",
                    LastUpdate = DateTime.UtcNow,
                    Markets = new List<OddsApiMarket>
                    {
                        new OddsApiMarket
                        {
                            Key = "h2h",
                            LastUpdate = DateTime.UtcNow,
                            Outcomes = new List<OddsApiOutcome>
                            {
                                new OddsApiOutcome { Name = "Dallas Cowboys", Price = 2.10m },
                                new OddsApiOutcome { Name = "Philadelphia Eagles", Price = 1.75m }
                            }
                        },
                        new OddsApiMarket
                        {
                            Key = "spreads",
                            LastUpdate = DateTime.UtcNow,
                            Outcomes = new List<OddsApiOutcome>
                            {
                                new OddsApiOutcome { Name = "Dallas Cowboys", Price = 1.91m, Point = 3.5m },
                                new OddsApiOutcome { Name = "Philadelphia Eagles", Price = 1.91m, Point = -3.5m }
                            }
                        },
                        new OddsApiMarket
                        {
                            Key = "totals",
                            LastUpdate = DateTime.UtcNow,
                            Outcomes = new List<OddsApiOutcome>
                            {
                                new OddsApiOutcome { Name = "Over", Price = 1.90m, Point = 45.5m },
                                new OddsApiOutcome { Name = "Under", Price = 1.90m, Point = 45.5m }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var markets = _marketMapper.MapMarketsForEvent(oddsEvent);

        // Assert
        markets.Should().HaveCount(3);
        markets.Should().Contain(m => m.Type == MarketType.Moneyline);
        markets.Should().Contain(m => m.Type == MarketType.Spread);
        markets.Should().Contain(m => m.Type == MarketType.Totals);

        // Verify total outcomes (2 per market = 6 total)
        markets.Sum(m => m.Outcomes.Count).Should().Be(6);
    }
}
