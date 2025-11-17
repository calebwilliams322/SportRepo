using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Configuration;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;
using Xunit;

namespace SportsBetting.Data.Tests;

/// <summary>
/// Integration tests for the complete commission system
/// Tests user registration, statistics tracking, settlement, and tier updates
/// </summary>
public class CommissionIntegrationTests : IDisposable
{
    private readonly SportsBettingDbContext _context;
    private readonly CommissionService _commissionService;
    private readonly SettlementService _settlementService;

    public CommissionIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SportsBettingDbContext(options);

        var config = new CommissionConfiguration();
        _commissionService = new CommissionService(config);
        _settlementService = new SettlementService(_commissionService);
    }

    [Fact]
    public void UserRegistration_CreatesUserStatistics_Successfully()
    {
        // Arrange & Act
        var user = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        var stats = new UserStatistics(user);

        _context.Users.Add(user);
        _context.UserStatistics.Add(stats);
        _context.SaveChanges();

        // Assert
        var savedStats = _context.UserStatistics.FirstOrDefault(s => s.UserId == user.Id);
        Assert.NotNull(savedStats);
        Assert.Equal(user.Id, savedStats.UserId);
        Assert.Equal(0, savedStats.TotalVolumeAllTime);
        Assert.Equal(CommissionTier.Standard, user.CommissionTier);
    }

    [Fact]
    public void SettleExchangeMatch_StandardTierMaker_ChargesCorrectCommission()
    {
        // Arrange
        var (backUser, layUser, match, outcome, market) = SetupMatchScenario(
            backUserTier: CommissionTier.Standard,
            layUserTier: CommissionTier.Standard,
            matchedStake: 100m,
            odds: 2.5m
        );

        // The back bet is the maker (was placed first)
        // Back bet wins
        market.Settle(outcome.Id);

        // Act
        var (winnerBet, netWinnings) = _settlementService.SettleExchangeMatch(
            match,
            outcome,
            backUser,
            layUser
        );

        // Assert
        var grossWinnings = 100m * (2.5m - 1); // $150
        var expectedCommission = grossWinnings * 0.012m; // 1.5% Standard - 20% maker = 1.2%
        var expectedNetWinnings = grossWinnings - expectedCommission;

        Assert.Equal(expectedNetWinnings, netWinnings.Amount);
        Assert.Equal(expectedCommission, match.BackBetCommission);
        Assert.Equal(0, match.LayBetCommission); // Loser pays no commission

        // Verify statistics updated
        Assert.Equal(expectedCommission, backUser.Statistics!.TotalCommissionPaidAllTime);
        Assert.Equal(expectedNetWinnings, backUser.Statistics.NetProfitAllTime);
        Assert.Equal(-100m, layUser.Statistics!.NetProfitAllTime); // Lost stake
    }

    [Fact]
    public void SettleExchangeMatch_PlatinumTierMaker_ChargesCorrectCommission()
    {
        // Arrange
        var (backUser, layUser, match, outcome, market) = SetupMatchScenario(
            backUserTier: CommissionTier.Platinum,
            layUserTier: CommissionTier.Standard,
            matchedStake: 1000m,
            odds: 3.0m
        );

        // Back bet wins (maker is Platinum tier)
        market.Settle(outcome.Id);

        // Act
        var (winnerBet, netWinnings) = _settlementService.SettleExchangeMatch(
            match,
            outcome,
            backUser,
            layUser
        );

        // Assert
        var grossWinnings = 1000m * (3.0m - 1); // $2,000
        var expectedCommission = grossWinnings * 0.004m; // 0.5% Platinum - 20% maker = 0.4%
        var expectedNetWinnings = grossWinnings - expectedCommission;

        Assert.Equal(expectedNetWinnings, netWinnings.Amount);
        Assert.Equal(expectedCommission, match.BackBetCommission);
        Assert.Equal(0, match.LayBetCommission); // Loser pays no commission

        // Verify winner statistics
        Assert.Equal(expectedCommission, backUser.Statistics!.TotalCommissionPaidAllTime);
        Assert.Equal(expectedNetWinnings, backUser.Statistics.NetProfitAllTime);
    }

    [Fact]
    public void UserStatistics_TracksMakerAndTakerTrades_Correctly()
    {
        // Arrange
        var user = User.CreateWithPassword("trader", "trader@test.com", "Password123!");
        var stats = new UserStatistics(user);

        // Act - Record various trades
        stats.RecordBetMatched(100m, isMaker: true);
        stats.RecordBetMatched(200m, isMaker: false);
        stats.RecordBetMatched(150m, isMaker: true);
        stats.RecordBetMatched(50m, isMaker: false);

        // Assert
        Assert.Equal(500m, stats.TotalVolumeAllTime);
        Assert.Equal(500m, stats.Volume30Day);
        Assert.Equal(2, stats.MakerTradesAllTime);
        Assert.Equal(2, stats.TakerTradesAllTime);
        Assert.Equal(250m, stats.MakerVolumeAllTime);
        Assert.Equal(250m, stats.TakerVolumeAllTime);
        Assert.Equal(50m, stats.MakerPercentage); // 50% maker
    }

    [Fact]
    public void CommissionService_UpdatesUserTier_BasedOnVolume()
    {
        // Arrange
        var user = User.CreateWithPassword("whale", "whale@test.com", "Password123!");
        var stats = new UserStatistics(user);

        // Start at Standard tier
        Assert.Equal(CommissionTier.Standard, user.CommissionTier);

        // Act 1: Trade $15k (should promote to Bronze)
        for (int i = 0; i < 15; i++)
        {
            stats.RecordBetMatched(1000m, isMaker: true);
        }

        var changed1 = _commissionService.UpdateUserTier(user);

        // Assert 1
        Assert.True(changed1);
        Assert.Equal(CommissionTier.Bronze, user.CommissionTier);

        // Act 2: Trade another $60k (total $75k, should promote to Silver)
        for (int i = 0; i < 60; i++)
        {
            stats.RecordBetMatched(1000m, isMaker: true);
        }

        var changed2 = _commissionService.UpdateUserTier(user);

        // Assert 2
        Assert.True(changed2);
        Assert.Equal(CommissionTier.Silver, user.CommissionTier);
        Assert.Equal(75000m, stats.Volume30Day);
    }

    [Fact]
    public void EndToEnd_NewUserToSettlement_WorksCorrectly()
    {
        // Arrange - Create two new users
        var alice = User.CreateWithPassword("alice", "alice@test.com", "Password123!");
        var bob = User.CreateWithPassword("bob", "bob@test.com", "Password123!");

        var aliceStats = new UserStatistics(alice);
        var bobStats = new UserStatistics(bob);

        _context.Users.AddRange(alice, bob);
        _context.UserStatistics.AddRange(aliceStats, bobStats);
        _context.SaveChanges();

        // Create event, market, and outcome
        var sport = new Sport("Basketball", "NBA Basketball");
        var league = new League("NBA", "NBA", sport.Id);
        var homeTeam = new Team("Lakers", "LAL", league.Id);
        var awayTeam = new Team("Celtics", "BOS", league.Id);

        var evt = new Event(
            "Lakers vs Celtics",
            homeTeam,
            awayTeam,
            DateTime.UtcNow.AddDays(1),
            league.Id,
            "Crypto.com Arena"
        );

        var market = new Market(MarketType.Moneyline, "Moneyline");
        evt.AddMarket(market);

        // Set market to Exchange mode for exchange betting
        typeof(Market)
            .GetProperty(nameof(Market.Mode))!
            .SetValue(market, MarketMode.Exchange);

        var outcome = new Outcome("Lakers Win", "Lakers Win", new Odds(2.5m));
        market.AddOutcome(outcome);

        // Add all entities to context
        // Don't save event entities - keep in memory like SetupMatchScenario
        // This avoids entity tracking issues with InMemory database

        // Create exchange bets (all in memory, no SaveChanges)
        var aliceWallet = new Wallet(alice);
        var bobWallet = new Wallet(bob);

        var aliceBet = Bet.CreateExchangeSingle(
            alice,
            new Money(100m, "USD"),
            evt,
            market,
            outcome,
            2.5m,
            BetSide.Back
        );

        var bobBet = Bet.CreateExchangeSingle(
            bob,
            new Money(100m, "USD"),
            evt,
            market,
            outcome,
            2.5m,
            BetSide.Lay
        );

        var aliceExchangeBet = new ExchangeBet(aliceBet, BetSide.Back, 2.5m, 100m);
        var bobExchangeBet = new ExchangeBet(bobBet, BetSide.Lay, 2.5m, 100m);

        // Record statistics for the match
        aliceStats.RecordBetPlaced(100m);
        bobStats.RecordBetPlaced(100m);
        aliceStats.RecordBetMatched(100m, isMaker: true); // Alice is maker
        bobStats.RecordBetMatched(100m, isMaker: false); // Bob is taker

        var match = new BetMatch(
            aliceExchangeBet,
            bobExchangeBet,
            100m,
            2.5m,
            aliceExchangeBet // Alice's bet was first (maker)
        );

        // Act - Lakers win, settle the match
        market.Settle(outcome.Id);

        var (winnerBet, netWinnings) = _settlementService.SettleExchangeMatch(
            match,
            outcome,
            alice,
            bob
        );

        // Assert
        // Note: We don't call SaveChanges here because the SettlementService modifies
        // entities in memory, and we're just verifying the calculations
        var grossWinnings = 100m * (2.5m - 1); // $150
        var expectedCommission = grossWinnings * 0.012m; // 1.2% (Standard maker)
        var expectedNetWinnings = grossWinnings - expectedCommission;

        // Verify settlement
        Assert.Equal(aliceBet, winnerBet);
        Assert.Equal(expectedNetWinnings, netWinnings.Amount);
        Assert.True(match.IsSettled);

        // Verify Alice's statistics
        Assert.Equal(100m, aliceStats.TotalVolumeAllTime);
        Assert.Equal(1, aliceStats.MakerTradesAllTime);
        Assert.Equal(expectedCommission, aliceStats.TotalCommissionPaidAllTime);
        Assert.Equal(expectedNetWinnings, aliceStats.NetProfitAllTime);

        // Verify Bob's statistics (loser)
        Assert.Equal(100m, bobStats.TotalVolumeAllTime);
        Assert.Equal(1, bobStats.TakerTradesAllTime);
        Assert.Equal(0m, bobStats.TotalCommissionPaidAllTime); // Losers don't pay commission
        Assert.Equal(-100m, bobStats.NetProfitAllTime); // Lost stake
    }

    [Fact]
    public void MultipleSettlements_AccumulatesStatistics_Correctly()
    {
        // Arrange
        var user = User.CreateWithPassword("active", "active@test.com", "Password123!");
        var stats = new UserStatistics(user);

        // Act - Simulate 10 winning trades
        for (int i = 0; i < 10; i++)
        {
            stats.RecordBetMatched(500m, isMaker: i % 2 == 0); // Alternate maker/taker

            var winnings = 250m; // Won $250 each trade
            var commission = winnings * (i % 2 == 0 ? 0.012m : 0.015m); // Maker vs taker

            stats.RecordCommissionPaid(commission);
            stats.RecordBetSettled(winnings - commission);
        }

        // Assert
        Assert.Equal(5000m, stats.TotalVolumeAllTime); // 10 × $500
        Assert.Equal(5, stats.MakerTradesAllTime);
        Assert.Equal(5, stats.TakerTradesAllTime);

        var expectedTotalCommission = (5 * 250m * 0.012m) + (5 * 250m * 0.015m); // $16.88
        Assert.Equal(expectedTotalCommission, stats.TotalCommissionPaidAllTime, 2);
    }

    private (User backUser, User layUser, BetMatch match, Outcome outcome, Market market) SetupMatchScenario(
        CommissionTier backUserTier,
        CommissionTier layUserTier,
        decimal matchedStake,
        decimal odds)
    {
        // Create users with specified tiers
        var backUser = User.CreateWithPassword("back_user", "back@test.com", "Password123!");
        var layUser = User.CreateWithPassword("lay_user", "lay@test.com", "Password123!");

        backUser.UpdateCommissionTier(backUserTier);
        layUser.UpdateCommissionTier(layUserTier);

        var backStats = new UserStatistics(backUser);
        var layStats = new UserStatistics(layUser);

        // Create event structure
        var sport = new Sport("Basketball", "NBA Basketball");
        var league = new League("NBA", "NBA", sport.Id);
        var homeTeam = new Team("Lakers", "LAL", league.Id);
        var awayTeam = new Team("Celtics", "BOS", league.Id);

        var evt = new Event(
            "Lakers vs Celtics",
            homeTeam,
            awayTeam,
            DateTime.UtcNow.AddDays(1),
            league.Id,
            "Crypto.com Arena"
        );

        var market = new Market(MarketType.Moneyline, "Moneyline");
        evt.AddMarket(market);

        // Set market to Exchange mode for exchange betting
        typeof(Market)
            .GetProperty(nameof(Market.Mode))!
            .SetValue(market, MarketMode.Exchange);

        var outcome = new Outcome("Lakers Win", "Lakers Win", new Odds(2.5m));
        market.AddOutcome(outcome);

        // Create wallets
        var backWallet = new Wallet(backUser);
        var layWallet = new Wallet(layUser);

        // Create bets
        var backBet = Bet.CreateExchangeSingle(
            backUser,
            new Money(matchedStake, "USD"),
            evt,
            market,
            outcome,
            odds,
            BetSide.Back
        );

        var layBet = Bet.CreateExchangeSingle(
            layUser,
            new Money(matchedStake, "USD"),
            evt,
            market,
            outcome,
            odds,
            BetSide.Lay
        );

        var backExchangeBet = new ExchangeBet(backBet, BetSide.Back, odds, matchedStake);
        var layExchangeBet = new ExchangeBet(layBet, BetSide.Lay, odds, matchedStake);

        // Back bet is maker (was placed first), lay bet is taker
        var match = new BetMatch(
            backExchangeBet,
            layExchangeBet,
            matchedStake,
            odds,
            backExchangeBet // Maker
        );

        return (backUser, layUser, match, outcome, market);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
