using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Tests;

/// <summary>
/// Integration tests for concurrent operations with optimistic concurrency control
/// These tests verify that xmin-based concurrency control prevents race conditions
/// </summary>
public class ConcurrencyTests : IDisposable
{
    private readonly SportsBettingDbContext _context;
    private readonly string _testDatabaseName;

    public ConcurrencyTests()
    {
        // Create a unique test database for each test run
        _testDatabaseName = $"sportsbetting_concurrency_test_{Guid.NewGuid():N}";

        var connectionString = Environment.GetEnvironmentVariable("SPORTSBETTING_DB")
            ?? "Host=localhost;Database=sportsbetting;Username=calebwilliams";

        // Create test database
        var masterConnectionString = connectionString.Replace("sportsbetting", "postgres");
        using (var masterContext = new DbContext(new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(masterConnectionString).Options))
        {
            // Drop database if it exists from a previous failed test run
            var dropDbSql = $"DROP DATABASE IF EXISTS \"{_testDatabaseName}\"";
            masterContext.Database.ExecuteSqlRaw(dropDbSql);

            // Create test database
            var createDbSql = $"CREATE DATABASE \"{_testDatabaseName}\"";
            masterContext.Database.ExecuteSqlRaw(createDbSql);
        }

        // Connect to test database and apply migrations
        var testConnectionString = connectionString.Replace("sportsbetting", _testDatabaseName);
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseNpgsql(testConnectionString)
            .Options;

        _context = new SportsBettingDbContext(options);
        _context.Database.Migrate();
    }

    [Fact]
    public async Task ConcurrentWithdrawals_ShouldThrowConcurrencyException()
    {
        // Arrange
        var user = new User("concurrency_test", "concurrency@test.com", "hash123");
        var wallet = new Wallet(user);
        var walletService = new WalletService();

        // Deposit initial balance
        walletService.Deposit(user, new Money(1000m, "USD"), "Initial deposit");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var walletId = wallet.Id;

        // Act & Assert - Simulate concurrent withdrawals
        var connectionString = _context.Database.GetConnectionString()!;

        // Create two separate contexts simulating two concurrent requests
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context1 = new SportsBettingDbContext(options);
        var context2 = new SportsBettingDbContext(options);

        try
        {
            // Both contexts load the same wallet (same xmin value)
            var user1 = await context1.Users.Include(u => u.Wallet).FirstAsync(u => u.Id == user.Id);
            var user2 = await context2.Users.Include(u => u.Wallet).FirstAsync(u => u.Id == user.Id);

            // Both try to withdraw
            var service1 = new WalletService();
            var service2 = new WalletService();

            var transaction1 = service1.Withdraw(user1, new Money(100m, "USD"), "Withdrawal 1");
            var transaction2 = service2.Withdraw(user2, new Money(100m, "USD"), "Withdrawal 2");

            context1.Transactions.Add(transaction1);
            context2.Transactions.Add(transaction2);

            // First save should succeed
            await context1.SaveChangesAsync();

            // Second save should throw DbUpdateConcurrencyException because xmin changed
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            {
                await context2.SaveChangesAsync();
            });

            // Verify final balance - only one withdrawal should have succeeded
            await context1.Entry(user1).ReloadAsync();
            await context1.Entry(user1.Wallet!).ReloadAsync();
            Assert.Equal(900m, user1.Wallet!.Balance.Amount);
        }
        finally
        {
            context1.Dispose();
            context2.Dispose();
        }
    }

    [Fact]
    public async Task ConcurrentBetPlacements_ShouldPreventDoubleDeduction()
    {
        // Arrange
        var user = new User("bettor", "bettor@test.com", "hash123");
        var wallet = new Wallet(user);
        var walletService = new WalletService();

        // Deposit initial balance
        walletService.Deposit(user, new Money(200m, "USD"), "Initial deposit");

        // Create a sport, league, teams, and event
        var sport = new Sport("Soccer", "SOC");
        var league = new League("Premier League", "EPL", sport.Id);
        var homeTeam = new Team("Arsenal", "ARS", league.Id);
        var awayTeam = new Team("Chelsea", "CHE", league.Id);
        var evt = new Event("Arsenal vs Chelsea", homeTeam, awayTeam, DateTime.UtcNow.AddDays(1), league.Id, "Emirates Stadium");
        var market = new Market(MarketType.Moneyline, "Match Winner");
        evt.AddMarket(market);
        var outcome = new Outcome("Arsenal Win", "Arsenal wins", new Odds(2.0m));
        market.AddOutcome(outcome);

        _context.Users.Add(user);
        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Act & Assert - Simulate concurrent bet placements
        var connectionString = _context.Database.GetConnectionString()!;
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context1 = new SportsBettingDbContext(options);
        var context2 = new SportsBettingDbContext(options);

        try
        {
            // Both contexts load the same user
            var user1 = await context1.Users.Include(u => u.Wallet).FirstAsync(u => u.Id == user.Id);
            var user2 = await context2.Users.Include(u => u.Wallet).FirstAsync(u => u.Id == user.Id);

            var evt1 = await context1.Events.Include(e => e.Markets).ThenInclude(m => m.Outcomes)
                .FirstAsync(e => e.Id == evt.Id);
            var evt2 = await context2.Events.Include(e => e.Markets).ThenInclude(m => m.Outcomes)
                .FirstAsync(e => e.Id == evt.Id);

            var market1 = evt1.Markets.First();
            var market2 = evt2.Markets.First();
            var outcome1 = market1.Outcomes.First();
            var outcome2 = market2.Outcomes.First();

            // Both try to place $150 bets (total would be $300, but wallet has only $200)
            var bet1 = Bet.CreateSingle(user1, new Money(150m, "USD"), evt1, market1, outcome1);
            var bet2 = Bet.CreateSingle(user2, new Money(150m, "USD"), evt2, market2, outcome2);

            var service1 = new WalletService();
            var service2 = new WalletService();

            var transaction1 = service1.PlaceBet(user1, bet1);
            var transaction2 = service2.PlaceBet(user2, bet2);

            context1.Bets.Add(bet1);
            context1.Transactions.Add(transaction1);
            context2.Bets.Add(bet2);
            context2.Transactions.Add(transaction2);

            // First save should succeed
            await context1.SaveChangesAsync();

            // Second save should throw DbUpdateConcurrencyException
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            {
                await context2.SaveChangesAsync();
            });

            // Verify only one bet was placed and balance is correct
            await context1.Entry(user1).ReloadAsync();
            await context1.Entry(user1.Wallet!).ReloadAsync();
            Assert.Equal(50m, user1.Wallet!.Balance.Amount); // 200 - 150 = 50
            Assert.Equal(150m, user1.Wallet.TotalBet.Amount);

            var betsCount = await context1.Bets.CountAsync();
            Assert.Equal(1, betsCount);
        }
        finally
        {
            context1.Dispose();
            context2.Dispose();
        }
    }

    [Fact]
    public async Task ConcurrentDeposits_ShouldAllSucceedWithRetry()
    {
        // Arrange
        var user = new User("depositor", "depositor@test.com", "hash123");
        var wallet = new Wallet(user);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userId = user.Id;
        var connectionString = _context.Database.GetConnectionString()!;

        // Act - Simulate 5 concurrent deposits with retry logic
        var tasks = new List<Task<bool>>();
        const int concurrentDeposits = 5;
        const decimal depositAmount = 100m;

        for (int i = 0; i < concurrentDeposits; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                const int maxRetries = 3;
                int retryCount = 0;

                while (retryCount < maxRetries)
                {
                    var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
                        .UseNpgsql(connectionString)
                        .Options;

                    using var context = new SportsBettingDbContext(options);

                    try
                    {
                        var userToUpdate = await context.Users
                            .Include(u => u.Wallet)
                            .FirstAsync(u => u.Id == userId);

                        var service = new WalletService();
                        var transaction = service.Deposit(
                            userToUpdate,
                            new Money(depositAmount, "USD"),
                            $"Concurrent deposit #{taskId}"
                        );

                        context.Transactions.Add(transaction);
                        await context.SaveChangesAsync();
                        return true; // Success
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            return false; // Failed after retries
                        }
                        // Small delay before retry
                        await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount));
                    }
                }
                return false;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - At least some deposits should succeed
        var successCount = results.Count(r => r);
        Assert.True(successCount >= 3, $"Expected at least 3 deposits to succeed, got {successCount}");

        // Verify final balance matches successful deposits
        await _context.Entry(user).ReloadAsync();
        await _context.Entry(wallet).ReloadAsync();

        var expectedBalance = successCount * depositAmount;
        Assert.Equal(expectedBalance, wallet.Balance.Amount);
        Assert.Equal(expectedBalance, wallet.TotalDeposited.Amount);
    }

    [Fact]
    public async Task ConcurrentBetSettlement_AndNewBet_ShouldBeHandledCorrectly()
    {
        // Arrange - This tests the real-world scenario where a bet settles while user places a new bet
        var user = new User("settler", "settler@test.com", "hash123");
        var wallet = new Wallet(user);
        var walletService = new WalletService();

        walletService.Deposit(user, new Money(1000m, "USD"), "Initial");

        var sport = new Sport("Tennis", "TEN");
        var league = new League("ATP Tour", "ATP", sport.Id);
        var player1 = new Team("Djokovic", "DJO", league.Id);
        var player2 = new Team("Nadal", "NAD", league.Id);
        var evt = new Event("Djokovic vs Nadal", player1, player2, DateTime.UtcNow.AddDays(1), league.Id, "Centre Court");
        var market = new Market(MarketType.Moneyline, "Match Winner");
        evt.AddMarket(market);
        var outcome = new Outcome("Djokovic Win", "Djokovic wins", new Odds(1.85m));
        market.AddOutcome(outcome);

        _context.Users.Add(user);
        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(player1, player2);
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Act - Concurrent operations: two deposits hitting same wallet
        var connectionString = _context.Database.GetConnectionString()!;
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        var context1 = new SportsBettingDbContext(options);
        var context2 = new SportsBettingDbContext(options);

        try
        {
            // Context 1: Deposit $500
            var user1 = await context1.Users.Include(u => u.Wallet).FirstAsync(u => u.Id == user.Id);
            var service1 = new WalletService();
            var transaction1 = service1.Deposit(user1, new Money(500m, "USD"), "Deposit 1");
            context1.Transactions.Add(transaction1);

            // Context 2: Place a bet (debits wallet)
            var user2 = await context2.Users.Include(u => u.Wallet).FirstAsync(u => u.Id == user.Id);
            var evt2 = await context2.Events.Include(e => e.Markets).ThenInclude(m => m.Outcomes)
                .FirstAsync(e => e.Id == evt.Id);
            var bet = Bet.CreateSingle(user2, new Money(200m, "USD"), evt2, evt2.Markets.First(), outcome);

            var service2 = new WalletService();
            var transaction2 = service2.PlaceBet(user2, bet);
            context2.Bets.Add(bet);
            context2.Transactions.Add(transaction2);

            // One should succeed, one should fail with concurrency exception
            await context1.SaveChangesAsync();

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            {
                await context2.SaveChangesAsync();
            });
        }
        finally
        {
            context1.Dispose();
            context2.Dispose();
        }
    }

    public void Dispose()
    {
        // Clean up test database
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
