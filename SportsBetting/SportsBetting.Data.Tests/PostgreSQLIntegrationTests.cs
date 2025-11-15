using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Tests;

/// <summary>
/// Integration tests that run against actual PostgreSQL database
/// These tests verify the full stack works end-to-end
/// </summary>
public class PostgreSQLIntegrationTests : IDisposable
{
    private readonly SportsBettingDbContext _context;
    private readonly string _testDatabaseName;

    public PostgreSQLIntegrationTests()
    {
        // Create a unique test database for each test run
        _testDatabaseName = $"sportsbetting_test_{Guid.NewGuid():N}";

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
    public void FullBettingWorkflow_EndToEnd()
    {
        // Arrange - Create a complete betting scenario
        var user = new User("fullworkflow", "workflow@test.com", "hash123");
        var wallet = new Wallet(user);
        var walletService = new WalletService();

        // Deposit funds
        var depositTx = walletService.Deposit(user, new Money(1000m, "USD"), "Initial deposit");

        // Create sport hierarchy
        var nba = new Sport("Basketball", "NBA");
        var westernConf = new League("Western Conference", "WEST", nba.Id);
        var lakers = new Team("Los Angeles Lakers", "LAL", westernConf.Id);
        var warriors = new Team("Golden State Warriors", "GSW", westernConf.Id);

        // Create event
        var game = new Event(
            "Lakers vs Warriors",
            lakers,
            warriors,
            DateTime.UtcNow.AddDays(1),
            westernConf.Id,
            "Crypto.com Arena"
        );

        // Create market with outcomes
        var moneyline = new Market(MarketType.Moneyline, "Game Winner");
        game.AddMarket(moneyline);

        var lakersWin = new Outcome("Lakers Win", "Lakers to win", new Odds(1.85m));
        var warriorsWin = new Outcome("Warriors Win", "Warriors to win", new Odds(2.10m));
        moneyline.AddOutcome(lakersWin);
        moneyline.AddOutcome(warriorsWin);

        // Place bet
        var bet = Bet.CreateSingle(user, new Money(200m, "USD"), game, moneyline, lakersWin);
        var betTx = walletService.PlaceBet(user, bet);

        // Act - Save everything to database
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.Transactions.AddRange(depositTx, betTx);
        _context.Sports.Add(nba);
        _context.Leagues.Add(westernConf);
        _context.Teams.AddRange(lakers, warriors);
        _context.Events.Add(game);
        _context.Bets.Add(bet);
        _context.SaveChanges();

        // Clear context to force fresh load
        _context.ChangeTracker.Clear();

        // Assert - Verify everything was persisted correctly
        var savedUser = _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefault(u => u.Username == "fullworkflow");

        Assert.NotNull(savedUser);
        Assert.NotNull(savedUser.Wallet);
        Assert.Equal(800m, savedUser.Wallet.Balance.Amount); // 1000 - 200 bet
        Assert.Equal(1000m, savedUser.Wallet.TotalDeposited.Amount);
        Assert.Equal(200m, savedUser.Wallet.TotalBet.Amount);

        var savedBet = _context.Bets
            .Include(b => b.Selections)
            .FirstOrDefault(b => b.UserId == savedUser.Id);

        Assert.NotNull(savedBet);
        Assert.Equal(BetType.Single, savedBet.Type);
        Assert.Equal(200m, savedBet.Stake.Amount);
        Assert.Equal(1.85m, savedBet.CombinedOdds.DecimalValue);
        Assert.Equal(370m, savedBet.PotentialPayout.Amount); // 200 * 1.85
        Assert.Single(savedBet.Selections);

        var transactions = _context.Transactions
            .Where(t => t.UserId == savedUser.Id)
            .OrderBy(t => t.CreatedAt)
            .ToList();

        Assert.Equal(2, transactions.Count);
        Assert.Equal(TransactionType.Deposit, transactions[0].Type);
        Assert.Equal(TransactionType.BetPlaced, transactions[1].Type);
        Assert.Equal(1000m, transactions[0].BalanceAfter.Amount);
        Assert.Equal(800m, transactions[1].BalanceAfter.Amount);
    }

    [Fact]
    public void EventLifecycle_CreateStartComplete_UpdatesCorrectly()
    {
        // Arrange
        var sport = new Sport("Tennis", "TEN");
        var league = new League("ATP", "ATP", sport.Id);
        var team1 = new Team("Federer", "FED", league.Id);
        var team2 = new Team("Nadal", "NAD", league.Id);
        var match = new Event("Federer vs Nadal", team1, team2,
            DateTime.UtcNow.AddDays(1), league.Id, "Wimbledon");
        var market = new Market(MarketType.Moneyline, "Winner");
        match.AddMarket(market);
        var outcome = new Outcome("Federer Win", "Federer wins", new Odds(2.0m));
        market.AddOutcome(outcome);

        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(team1, team2);
        _context.Events.Add(match);
        _context.SaveChanges();

        // Act - Progress event through lifecycle
        Assert.Equal(EventStatus.Scheduled, match.Status);

        match.Start();
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        var startedEvent = _context.Events.Find(match.Id);
        Assert.NotNull(startedEvent);
        Assert.Equal(EventStatus.InProgress, startedEvent.Status);

        startedEvent.Complete(new Score(6, 4));
        _context.SaveChanges();
        _context.ChangeTracker.Clear();

        var completedEvent = _context.Events.Find(match.Id);
        Assert.NotNull(completedEvent);
        Assert.Equal(EventStatus.Completed, completedEvent.Status);
        Assert.NotNull(completedEvent.FinalScore);
        Assert.Equal(6, completedEvent.FinalScore.Value.HomeScore);
        Assert.Equal(4, completedEvent.FinalScore.Value.AwayScore);
    }

    [Fact]
    public void TransactionHistory_AcrossMultipleOperations_PersistsCorrectly()
    {
        // Arrange
        var user = new User("txhistory", "txhistory@test.com", "hash");
        var wallet = new Wallet(user);
        var walletService = new WalletService();

        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act - Perform multiple wallet operations
        var tx1 = walletService.Deposit(user, new Money(1000m, "USD"), "Initial deposit");
        _context.Transactions.Add(tx1);
        _context.SaveChanges();

        var tx2 = walletService.Deposit(user, new Money(500m, "USD"), "Second deposit");
        _context.Transactions.Add(tx2);
        _context.SaveChanges();

        var tx3 = walletService.Withdraw(user, new Money(200m, "USD"), "Withdrawal");
        _context.Transactions.Add(tx3);
        _context.SaveChanges();

        // Assert
        _context.ChangeTracker.Clear();
        var transactions = _context.Transactions
            .Where(t => t.UserId == user.Id)
            .OrderBy(t => t.CreatedAt)
            .ToList();

        Assert.Equal(3, transactions.Count);
        Assert.Equal(1000m, transactions[0].BalanceAfter.Amount);
        Assert.Equal(1500m, transactions[1].BalanceAfter.Amount);
        Assert.Equal(1300m, transactions[2].BalanceAfter.Amount);

        var finalWallet = _context.Wallets.Find(wallet.Id);
        Assert.NotNull(finalWallet);
        Assert.Equal(1300m, finalWallet.Balance.Amount);
    }

    [Fact]
    public void ComplexQuery_JoiningMultipleTables_ReturnsCorrectData()
    {
        // Arrange - Create multiple users with bets
        var users = new[]
        {
            new User("user1", "user1@test.com", "hash"),
            new User("user2", "user2@test.com", "hash"),
            new User("user3", "user3@test.com", "hash")
        };

        var sport = new Sport("Football", "FB");
        var league = new League("NFL", "NFL", sport.Id);
        var team1 = new Team("Patriots", "PAT", league.Id);
        var team2 = new Team("Chiefs", "KC", league.Id);
        var game = new Event("Patriots vs Chiefs", team1, team2,
            DateTime.UtcNow.AddDays(1), league.Id, "Gillette Stadium");
        var market = new Market(MarketType.Moneyline, "Winner");
        game.AddMarket(market);
        var outcome = new Outcome("Patriots Win", "Patriots win", new Odds(1.90m));
        market.AddOutcome(outcome);

        var walletService = new WalletService();
        foreach (var user in users)
        {
            var wallet = new Wallet(user);
            wallet.Deposit(new Money(1000m, "USD"));
            _context.Users.Add(user);
            _context.Wallets.Add(wallet);

            var bet = Bet.CreateSingle(user, new Money(100m * (Array.IndexOf(users, user) + 1), "USD"),
                game, market, outcome);
            var betTx = walletService.PlaceBet(user, bet);
            _context.Bets.Add(bet);
            _context.Transactions.Add(betTx);
        }

        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(team1, team2);
        _context.Events.Add(game);
        _context.SaveChanges();

        // Act - Complex query joining users, bets, and wallets
        var results = (from u in _context.Users
                      join w in _context.Wallets on u.Id equals w.UserId
                      join b in _context.Bets on u.Id equals b.UserId
                      where b.Status == BetStatus.Pending
                      orderby b.Stake.Amount descending
                      select new
                      {
                          Username = u.Username,
                          WalletBalance = w.Balance.Amount,
                          BetStake = b.Stake.Amount,
                          PotentialWin = b.PotentialPayout.Amount
                      }).ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("user3", results[0].Username);
        Assert.Equal(300m, results[0].BetStake);
        Assert.Equal(700m, results[0].WalletBalance);

        Assert.Equal("user2", results[1].Username);
        Assert.Equal(200m, results[1].BetStake);
        Assert.Equal(800m, results[1].WalletBalance);

        Assert.Equal("user1", results[2].Username);
        Assert.Equal(100m, results[2].BetStake);
        Assert.Equal(900m, results[2].WalletBalance);
    }

    [Fact]
    public void ValueObjects_PersistAndRetrieveCorrectly()
    {
        // Arrange
        var user = new User("valueobjects", "vo@test.com", "hash", "EUR");
        var wallet = new Wallet(user);
        wallet.Deposit(new Money(500.75m, "EUR"));
        wallet.Withdraw(new Money(125.25m, "EUR"));

        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act
        _context.ChangeTracker.Clear();
        var retrieved = _context.Wallets
            .Include(w => w.User)
            .FirstOrDefault(w => w.UserId == user.Id);

        // Assert - Verify all Money value objects
        Assert.NotNull(retrieved);
        Assert.Equal(375.50m, retrieved.Balance.Amount);
        Assert.Equal("EUR", retrieved.Balance.Currency);
        Assert.Equal(500.75m, retrieved.TotalDeposited.Amount);
        Assert.Equal("EUR", retrieved.TotalDeposited.Currency);
        Assert.Equal(125.25m, retrieved.TotalWithdrawn.Amount);
        Assert.Equal("EUR", retrieved.TotalWithdrawn.Currency);
    }

    public void Dispose()
    {
        // Drop test database
        var connectionString = _context.Database.GetConnectionString()!.Replace(_testDatabaseName, "postgres");
        _context.Dispose();

        // Small delay to ensure connections are closed
        Thread.Sleep(100);

        using var masterContext = new DbContext(new DbContextOptionsBuilder<DbContext>()
            .UseNpgsql(connectionString).Options);

        try
        {
            masterContext.Database.ExecuteSql($"DROP DATABASE \"{_testDatabaseName}\" WITH (FORCE)");
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }
}
