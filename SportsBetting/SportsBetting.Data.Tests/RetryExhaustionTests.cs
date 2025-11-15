using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Tests;

/// <summary>
/// Tests that verify the system behaves correctly when all retries are exhausted
/// These tests ensure graceful failure under extreme contention
/// </summary>
public class RetryExhaustionTests : IDisposable
{
    private readonly SportsBettingDbContext _context;
    private readonly string _testDatabaseName;

    public RetryExhaustionTests()
    {
        // Create a unique test database for each test run
        _testDatabaseName = $"sportsbetting_retry_test_{Guid.NewGuid():N}";

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
    public async Task ExtremeContention_SomeOperationsFail_ButDataRemainsConsistent()
    {
        // Arrange - Create user with $1000
        var user = new User("extreme_user", "extreme@test.com", "hash123");
        var wallet = new Wallet(user);
        var walletService = new WalletService();
        walletService.Deposit(user, new Money(1000m, "USD"), "Initial");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userId = user.Id;
        var connectionString = _context.Database.GetConnectionString()!;

        // Act - 20 concurrent $100 withdrawals (total would be $2000, but only have $1000)
        // This creates extreme contention and some will exhaust retries
        var tasks = new List<Task<bool>>();
        const int concurrentWithdrawals = 20;

        for (int i = 0; i < concurrentWithdrawals; i++)
        {
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

                        // Try to withdraw
                        var transaction = service.Withdraw(userToUpdate, new Money(100m, "USD"), "Concurrent withdrawal");
                        context.Transactions.Add(transaction);
                        await context.SaveChangesAsync();
                        return true; // Success
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            return false; // Failed after all retries
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount));
                    }
                    catch (Exception)
                    {
                        // Insufficient funds or other domain exception
                        return false;
                    }
                }
                return false;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r);
        var failureCount = results.Count(r => !r);

        // Max 10 withdrawals can succeed (10 * $100 = $1000)
        Assert.True(successCount <= 10, $"No more than 10 withdrawals should succeed, got {successCount}");
        Assert.True(failureCount > 0, "Some withdrawals should fail due to exhausted retries or insufficient funds");

        // CRITICAL: Verify final balance is consistent
        await _context.Entry(user).ReloadAsync();
        await _context.Entry(wallet).ReloadAsync();

        var expectedBalance = 1000m - (successCount * 100m);
        Assert.Equal(expectedBalance, wallet.Balance.Amount);

        Console.WriteLine($"Extreme contention: {successCount} succeeded, {failureCount} failed");
    }

    [Fact]
    public async Task HighContentionBets_FailuresDontCorruptData()
    {
        // Arrange
        var user = new User("bet_user", "bet@test.com", "hash123");
        var wallet = new Wallet(user);
        var walletService = new WalletService();
        walletService.Deposit(user, new Money(300m, "USD"), "Initial");

        var sport = new Sport("Soccer", "SOC");
        var league = new League("Premier League", "EPL", sport.Id);
        var homeTeam = new Team("Arsenal", "ARS", league.Id);
        var awayTeam = new Team("Chelsea", "CHE", league.Id);
        var evt = new Event("Arsenal vs Chelsea", homeTeam, awayTeam, DateTime.UtcNow.AddDays(1), league.Id, "Emirates");
        var market = new Market(MarketType.Moneyline, "Winner");
        evt.AddMarket(market);
        var outcome = new Outcome("Arsenal Win", "Arsenal wins", new Odds(2.0m));
        market.AddOutcome(outcome);

        _context.Users.Add(user);
        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var userId = user.Id;
        var eventId = evt.Id;
        var marketId = market.Id;
        var outcomeId = outcome.Id;
        var connectionString = _context.Database.GetConnectionString()!;

        // Act - 10 concurrent $100 bets (total $1000, but only have $300)
        var tasks = new List<Task<bool>>();

        for (int i = 0; i < 10; i++)
        {
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

                        var evtToUpdate = await context.Events
                            .Include(e => e.Markets)
                            .ThenInclude(m => m.Outcomes)
                            .FirstAsync(e => e.Id == eventId);

                        var marketToUse = evtToUpdate.Markets.First(m => m.Id == marketId);
                        var outcomeToUse = marketToUse.Outcomes.First(o => o.Id == outcomeId);

                        var bet = Bet.CreateSingle(userToUpdate, new Money(100m, "USD"), evtToUpdate, marketToUse, outcomeToUse);

                        var service = new WalletService();
                        var transaction = service.PlaceBet(userToUpdate, bet);

                        context.Bets.Add(bet);
                        context.Transactions.Add(transaction);
                        await context.SaveChangesAsync();
                        return true;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            return false;
                        }
                        await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount));
                    }
                    catch (Exception)
                    {
                        return false; // Insufficient funds
                    }
                }
                return false;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r);

        // Max 3 bets can succeed (3 * $100 = $300)
        Assert.True(successCount <= 3, $"No more than 3 bets should succeed, got {successCount}");

        // Verify data integrity
        await _context.Entry(user).ReloadAsync();
        await _context.Entry(wallet).ReloadAsync();

        var expectedBalance = 300m - (successCount * 100m);
        Assert.Equal(expectedBalance, wallet.Balance.Amount);

        var actualBetCount = await _context.Bets.CountAsync();
        Assert.Equal(successCount, actualBetCount);

        Console.WriteLine($"High contention bets: {successCount} bets succeeded with data integrity maintained");
    }

    public void Dispose()
    {
        // Clean up test database
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
