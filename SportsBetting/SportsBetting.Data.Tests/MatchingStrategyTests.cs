using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;
using Xunit;

namespace SportsBetting.Data.Tests;

/// <summary>
/// Unit tests for matching strategies
/// Tests FIFO, Pro-Rata, and Hybrid (Pro-Rata with Top) strategies
/// </summary>
public class MatchingStrategyTests
{
    // Helper method to create a mock ExchangeBet
    private ExchangeBet CreateMockBet(decimal unmatchedStake, DateTime createdAt)
    {
        // Create minimal mock objects for testing
        var user = new User("testuser", "test@test.com", "hash", "USD");
        var wallet = new Wallet(user);

        // Create dummy event/market/outcome for bet creation
        var sport = new Sport("Basketball", "NBA");
        var league = new League("Test League", "TEST", sport.Id);
        var team1 = new Team("Team A", "TA", league.Id);
        var team2 = new Team("Team B", "TB", league.Id);
        var gameEvent = new Event("Test Game", team1, team2,
            DateTime.UtcNow.AddDays(1), league.Id, "Test Arena");
        var market = new Market(MarketType.Moneyline, "Moneyline");

        // Use reflection to set market mode to Exchange for testing
        typeof(Market)
            .GetProperty(nameof(Market.Mode))!
            .SetValue(market, MarketMode.Exchange);

        gameEvent.AddMarket(market);
        var outcome = new Outcome("Team A Win", "Team A wins", new Odds(2.50m));
        market.AddOutcome(outcome);

        // Create bet using factory method
        var bet = Bet.CreateExchangeSingle(
            user,
            new Money(unmatchedStake, "USD"),
            gameEvent,
            market,
            outcome,
            2.50m,
            BetSide.Lay
        );

        var exchangeBet = new ExchangeBet(
            bet,
            BetSide.Lay,
            2.50m,
            unmatchedStake
        );

        // Use reflection to set CreatedAt for testing purposes
        typeof(ExchangeBet)
            .GetProperty(nameof(ExchangeBet.CreatedAt))!
            .SetValue(exchangeBet, createdAt);

        return exchangeBet;
    }

    [Fact]
    public void FifoStrategy_BasicScenario_AllocatesInOrder()
    {
        // Arrange
        var strategy = new FifoMatchingStrategy();
        var now = DateTime.UtcNow;

        var candidates = new List<ExchangeBet>
        {
            CreateMockBet(100m, now),                          // Alice: $100
            CreateMockBet(50m, now.AddSeconds(1)),            // Bob: $50
            CreateMockBet(100m, now.AddSeconds(2))            // Charlie: $100
        };

        decimal incomingStake = 150m;

        // Act
        var allocations = strategy.AllocateMatches(incomingStake, candidates);

        // Assert
        Assert.Equal(2, allocations.Count); // Only Alice and Bob get allocations (Charlie gets nothing)
        Assert.Equal(100m, allocations[candidates[0]]); // Alice: $100
        Assert.Equal(50m, allocations[candidates[1]]);  // Bob: $50
        Assert.False(allocations.ContainsKey(candidates[2])); // Charlie: not in dictionary (no allocation)
    }

    [Fact]
    public void FifoStrategy_OverflowScenario_FillsAllOrders()
    {
        // Arrange
        var strategy = new FifoMatchingStrategy();
        var now = DateTime.UtcNow;

        var candidates = new List<ExchangeBet>
        {
            CreateMockBet(100m, now),
            CreateMockBet(50m, now.AddSeconds(1))
        };

        decimal incomingStake = 200m; // More than available

        // Act
        var allocations = strategy.AllocateMatches(incomingStake, candidates);

        // Assert
        Assert.Equal(2, allocations.Count);
        Assert.Equal(100m, allocations[candidates[0]]); // Alice: $100 (full)
        Assert.Equal(50m, allocations[candidates[1]]);  // Bob: $50 (full)
        Assert.Equal(150m, allocations.Values.Sum());   // Total: $150 (not $200)
    }

    [Fact]
    public void ProRataStrategy_BasicScenario_AllocatesProportionally()
    {
        // Arrange
        var strategy = new ProRataMatchingStrategy();
        var now = DateTime.UtcNow;

        var candidates = new List<ExchangeBet>
        {
            CreateMockBet(100m, now),                          // Alice: $100 (40%)
            CreateMockBet(50m, now.AddSeconds(1)),            // Bob: $50 (20%)
            CreateMockBet(100m, now.AddSeconds(2))            // Charlie: $100 (40%)
        };
        // Total: $250

        decimal incomingStake = 150m;

        // Act
        var allocations = strategy.AllocateMatches(incomingStake, candidates);

        // Assert
        Assert.Equal(3, allocations.Count);

        // Expected: 40%, 20%, 40% of $150
        Assert.Equal(60m, allocations[candidates[0]]);  // Alice: 40% of $150 = $60
        Assert.Equal(30m, allocations[candidates[1]]);  // Bob: 20% of $150 = $30
        Assert.Equal(60m, allocations[candidates[2]]);  // Charlie: 40% of $150 = $60

        // Total should equal incoming stake
        Assert.Equal(150m, allocations.Values.Sum());
    }

    [Fact]
    public void ProRataStrategy_OverflowScenario_FillsAllOrders()
    {
        // Arrange
        var strategy = new ProRataMatchingStrategy();
        var now = DateTime.UtcNow;

        var candidates = new List<ExchangeBet>
        {
            CreateMockBet(100m, now),
            CreateMockBet(50m, now.AddSeconds(1))
        };

        decimal incomingStake = 200m; // More than available

        // Act
        var allocations = strategy.AllocateMatches(incomingStake, candidates);

        // Assert
        Assert.Equal(2, allocations.Count);
        Assert.Equal(100m, allocations[candidates[0]]); // Alice: $100 (full)
        Assert.Equal(50m, allocations[candidates[1]]);  // Bob: $50 (full)
        Assert.Equal(150m, allocations.Values.Sum());
    }

    [Fact]
    public void HybridStrategy_BasicScenario_CombinesFIFO_AndProRata()
    {
        // Arrange
        var strategy = new ProRataWithTopMatchingStrategy(
            topOrderCount: 1,
            topAllocationPercent: 0.40m
        );
        var now = DateTime.UtcNow;

        var candidates = new List<ExchangeBet>
        {
            CreateMockBet(100m, now),                          // Alice: $100 (TOP)
            CreateMockBet(50m, now.AddSeconds(1)),            // Bob: $50
            CreateMockBet(100m, now.AddSeconds(2))            // Charlie: $100
        };

        decimal incomingStake = 150m;

        // Act
        var allocations = strategy.AllocateMatches(incomingStake, candidates);

        // Assert
        Assert.Equal(3, allocations.Count);

        // Phase 1: 40% FIFO = $60 goes to Alice (top order)
        // Phase 2: 60% Pro-Rata = $90 distributed:
        //   - Alice remaining: $40 (21% of $190)
        //   - Bob: $50 (26% of $190)
        //   - Charlie: $100 (53% of $190)

        var aliceTotal = allocations[candidates[0]];
        var bobTotal = allocations[candidates[1]];
        var charlieTotal = allocations[candidates[2]];

        // Alice should get more than pro-rata (rewarded for being first)
        Assert.True(aliceTotal > 60m, $"Alice should get > $60, got ${aliceTotal}");
        Assert.True(aliceTotal < 100m, $"Alice should get < $100, got ${aliceTotal}");

        // Bob and Charlie should get less than Alice
        Assert.True(bobTotal < aliceTotal, "Bob should get less than Alice");
        Assert.True(charlieTotal > bobTotal, "Charlie should get more than Bob (larger order)");

        // Total should equal incoming stake
        Assert.Equal(150m, allocations.Values.Sum());
    }

    [Fact]
    public void HybridStrategy_AggressiveFIFO_PrioritizesTopOrders()
    {
        // Arrange (70% FIFO, 30% Pro-Rata)
        var strategy = new ProRataWithTopMatchingStrategy(
            topOrderCount: 2,
            topAllocationPercent: 0.70m
        );
        var now = DateTime.UtcNow;

        var candidates = new List<ExchangeBet>
        {
            CreateMockBet(100m, now),                          // Alice
            CreateMockBet(50m, now.AddSeconds(1)),            // Bob
            CreateMockBet(100m, now.AddSeconds(2))            // Charlie
        };

        decimal incomingStake = 150m;

        // Act
        var allocations = strategy.AllocateMatches(incomingStake, candidates);

        // Assert
        var aliceTotal = allocations[candidates[0]];
        var bobTotal = allocations[candidates[1]];

        // With 70% FIFO and top 2 orders, Alice and Bob should get priority
        // 70% of $150 = $105 allocated FIFO
        // Alice should get $100 (her full amount)
        // Bob should get $5
        // Remaining 30% of $150 = $45 distributed pro-rata

        Assert.True(aliceTotal >= 100m, $"Alice should get at least $100, got ${aliceTotal}");
        Assert.Equal(150m, allocations.Values.Sum());
    }

    [Fact]
    public void HybridStrategy_AggressiveProRata_FavorsLargeOrders()
    {
        // Arrange (20% FIFO, 80% Pro-Rata)
        var strategy = new ProRataWithTopMatchingStrategy(
            topOrderCount: 1,
            topAllocationPercent: 0.20m
        );
        var now = DateTime.UtcNow;

        var candidates = new List<ExchangeBet>
        {
            CreateMockBet(10m, now),                           // Alice (small)
            CreateMockBet(10m, now.AddSeconds(1)),            // Bob (small)
            CreateMockBet(1000m, now.AddSeconds(2))           // Charlie (LARGE)
        };

        decimal incomingStake = 500m;

        // Act
        var allocations = strategy.AllocateMatches(incomingStake, candidates);

        // Assert
        var aliceTotal = allocations[candidates[0]];
        var bobTotal = allocations[candidates[1]];
        var charlieTotal = allocations[candidates[2]];

        // Charlie should get vast majority (provides 98% of liquidity)
        Assert.True(charlieTotal > 450m, $"Charlie should get > $450, got ${charlieTotal}");
        Assert.True(aliceTotal < 30m, $"Alice should get < $30, got ${aliceTotal}");

        Assert.Equal(500m, allocations.Values.Sum());
    }

    [Fact]
    public void AllStrategies_EmptyOrderBook_ReturnEmptyAllocations()
    {
        // Arrange
        var candidates = new List<ExchangeBet>(); // Empty
        decimal incomingStake = 100m;

        // Act & Assert - FIFO
        var fifoAllocations = new FifoMatchingStrategy().AllocateMatches(incomingStake, candidates);
        Assert.Empty(fifoAllocations);

        // Act & Assert - Pro-Rata
        var proRataAllocations = new ProRataMatchingStrategy().AllocateMatches(incomingStake, candidates);
        Assert.Empty(proRataAllocations);

        // Act & Assert - Hybrid
        var hybridAllocations = new ProRataWithTopMatchingStrategy().AllocateMatches(incomingStake, candidates);
        Assert.Empty(hybridAllocations);
    }

    [Fact]
    public void AllStrategies_ZeroIncoming_ReturnEmptyAllocations()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var candidates = new List<ExchangeBet>
        {
            CreateMockBet(100m, now)
        };
        decimal incomingStake = 0m;

        // Act & Assert - FIFO
        var fifoAllocations = new FifoMatchingStrategy().AllocateMatches(incomingStake, candidates);
        Assert.Empty(fifoAllocations);

        // Act & Assert - Pro-Rata
        var proRataAllocations = new ProRataMatchingStrategy().AllocateMatches(incomingStake, candidates);
        Assert.True(proRataAllocations.Values.Sum() == 0m);

        // Act & Assert - Hybrid
        var hybridAllocations = new ProRataWithTopMatchingStrategy().AllocateMatches(incomingStake, candidates);
        Assert.True(hybridAllocations.Values.Sum() == 0m);
    }

    [Fact]
    public void AllStrategies_SingleOrder_AllocatesCorrectly()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var candidates = new List<ExchangeBet>
        {
            CreateMockBet(100m, now)
        };
        decimal incomingStake = 50m;

        // Act & Assert - FIFO
        var fifoAllocations = new FifoMatchingStrategy().AllocateMatches(incomingStake, candidates);
        Assert.Equal(50m, fifoAllocations[candidates[0]]);

        // Act & Assert - Pro-Rata
        var proRataAllocations = new ProRataMatchingStrategy().AllocateMatches(incomingStake, candidates);
        Assert.Equal(50m, proRataAllocations[candidates[0]]);

        // Act & Assert - Hybrid
        var hybridAllocations = new ProRataWithTopMatchingStrategy().AllocateMatches(incomingStake, candidates);
        Assert.Equal(50m, hybridAllocations[candidates[0]]);
    }
}
