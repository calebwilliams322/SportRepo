using Microsoft.EntityFrameworkCore;
using Npgsql;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Tests;

/// <summary>
/// Tests that verify database-level constraints are working correctly
/// These constraints prevent invalid data from being persisted
/// </summary>
public class DatabaseConstraintTests : IDisposable
{
    private readonly SportsBettingDbContext _context;
    private readonly string _testDatabaseName;

    public DatabaseConstraintTests()
    {
        // Create a unique test database for each test run
        _testDatabaseName = $"sportsbetting_constraint_test_{Guid.NewGuid():N}";

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
    public async Task WalletBalance_CannotBeNegative_ThrowsException()
    {
        // Arrange
        var user = new User("constraint_test", "constraint@test.com", "hash123");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act & Assert - Try to create negative balance by direct SQL manipulation
        var walletId = wallet.Id;
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await _context.Database.ExecuteSqlRawAsync(
                $"UPDATE \"Wallets\" SET \"Balance\" = -100 WHERE \"Id\" = '{walletId}'");
        });

        Assert.Contains("CK_Wallets_Balance_NonNegative", exception.Message);
    }

    [Fact]
    public async Task Transaction_CannotHaveZeroOrNegativeAmount_ThrowsException()
    {
        // Arrange
        var user = new User("txn_test", "txn@test.com", "hash123");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act & Assert - Try to insert transaction with zero amount
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await _context.Database.ExecuteSqlRawAsync(
                $@"INSERT INTO ""Transactions""
                (""Id"", ""UserId"", ""Type"", ""Status"", ""Amount"", ""Currency"",
                 ""BalanceBefore"", ""BalanceBeforeCurrency"", ""BalanceAfter"", ""BalanceAfterCurrency"",
                 ""Description"", ""CreatedAt"")
                VALUES ('{Guid.NewGuid()}', '{user.Id}', 'Deposit', 'Completed', 0, 'USD',
                        0, 'USD', 0, 'USD', 'Invalid', NOW())");
        });

        Assert.Contains("CK_Transactions_Amount_Positive", exception.Message);
    }

    [Fact]
    public async Task Bet_CannotHaveNegativeStake_ThrowsException()
    {
        // Arrange
        var user = new User("bet_test", "bet@test.com", "hash123");
        var wallet = new Wallet(user);
        var walletService = new WalletService();
        walletService.Deposit(user, new Money(1000m, "USD"), "Initial");

        var sport = new Sport("Basketball", "BBL");
        var league = new League("NBA", "NBA", sport.Id);
        var homeTeam = new Team("Lakers", "LAL", league.Id);
        var awayTeam = new Team("Celtics", "BOS", league.Id);
        var evt = new Event("Lakers vs Celtics", homeTeam, awayTeam, DateTime.UtcNow.AddDays(1), league.Id, "Staples Center");
        var market = new Market(MarketType.Moneyline, "Winner");
        evt.AddMarket(market);
        var outcome = new Outcome("Lakers Win", "Lakers win", new Odds(2.0m));
        market.AddOutcome(outcome);

        _context.Users.Add(user);
        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(homeTeam, awayTeam);
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        // Act & Assert - Try to insert bet with negative stake
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await _context.Database.ExecuteSqlRawAsync(
                $@"INSERT INTO ""Bets""
                (""Id"", ""UserId"", ""TicketNumber"", ""Type"", ""Status"", ""Stake"", ""StakeCurrency"",
                 ""PotentialPayout"", ""PotentialPayoutCurrency"", ""CombinedOddsDecimal"", ""PlacedAt"")
                VALUES ('{Guid.NewGuid()}', '{user.Id}', 'INVALID', 'Single', 'Pending', -50, 'USD',
                        100, 'USD', 2.0, NOW())");
        });

        Assert.Contains("CK_Bets_Stake_Positive", exception.Message);
    }

    [Fact]
    public async Task Bet_CannotHaveOddsLessThanOne_ThrowsException()
    {
        // Arrange
        var user = new User("odds_test", "odds@test.com", "hash123");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act & Assert - Try to insert bet with odds less than 1.0
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await _context.Database.ExecuteSqlRawAsync(
                $@"INSERT INTO ""Bets""
                (""Id"", ""UserId"", ""TicketNumber"", ""Type"", ""Status"", ""Stake"", ""StakeCurrency"",
                 ""PotentialPayout"", ""PotentialPayoutCurrency"", ""CombinedOddsDecimal"", ""PlacedAt"")
                VALUES ('{Guid.NewGuid()}', '{user.Id}', 'INVALID2', 'Single', 'Pending', 50, 'USD',
                        25, 'USD', 0.5, NOW())");
        });

        Assert.Contains("CK_Bets_CombinedOdds_MinimumOne", exception.Message);
    }

    [Fact]
    public async Task Transaction_BalancesCannotBeNegative_ThrowsException()
    {
        // Arrange
        var user = new User("balance_test", "balance@test.com", "hash123");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act & Assert - Try to insert transaction with negative BalanceAfter
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await _context.Database.ExecuteSqlRawAsync(
                $@"INSERT INTO ""Transactions""
                (""Id"", ""UserId"", ""Type"", ""Status"", ""Amount"", ""Currency"",
                 ""BalanceBefore"", ""BalanceBeforeCurrency"", ""BalanceAfter"", ""BalanceAfterCurrency"",
                 ""Description"", ""CreatedAt"")
                VALUES ('{Guid.NewGuid()}', '{user.Id}', 'Withdrawal', 'Completed', 100, 'USD',
                        50, 'USD', -50, 'USD', 'Overdraft attempt', NOW())");
        });

        Assert.Contains("CK_Transactions_BalanceAfter_NonNegative", exception.Message);
    }

    [Fact]
    public async Task Wallet_TotalDepositedCannotBeNegative_ThrowsException()
    {
        // Arrange
        var user = new User("totals_test", "totals@test.com", "hash123");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act & Assert
        var walletId = wallet.Id;
        var exception = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await _context.Database.ExecuteSqlRawAsync(
                $"UPDATE \"Wallets\" SET \"TotalDeposited\" = -100 WHERE \"Id\" = '{walletId}'");
        });

        Assert.Contains("CK_Wallets_TotalDeposited_NonNegative", exception.Message);
    }

    public void Dispose()
    {
        // Clean up test database
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
