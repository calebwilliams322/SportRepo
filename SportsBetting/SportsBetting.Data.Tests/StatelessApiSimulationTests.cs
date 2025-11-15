using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Tests;

/// <summary>
/// Tests that simulate how a stateless API will work:
/// 1. Load entity from database
/// 2. Execute business logic
/// 3. Save back to database
/// 4. Dispose everything (simulate end of HTTP request)
/// 5. Repeat for next request
/// </summary>
public class StatelessApiSimulationTests : IDisposable
{
    private readonly SportsBettingDbContext _context;

    public StatelessApiSimulationTests()
    {
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new SportsBettingDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void StatelessAPI_CreateUser_ThenLoadAndDeposit_ThenLoadAndWithdraw()
    {
        Guid userId;
        Guid walletId;

        // ============================================
        // REQUEST 1: Create user (like POST /api/users)
        // ============================================
        {
            var user = new User("apiuser", "api@test.com", "hash123");
            var wallet = new Wallet(user);

            _context.Users.Add(user);
            _context.Wallets.Add(wallet);
            _context.SaveChanges();

            userId = user.Id;
            walletId = wallet.Id;

            // Simulate end of HTTP request - clear all tracked entities
            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 2: Deposit money (like POST /api/wallets/{id}/deposit)
        // ============================================
        {
            // Load fresh from database (simulates new HTTP request)
            var user = _context.Users.Include(u => u.Wallet).FirstOrDefault(u => u.Id == userId);
            Assert.NotNull(user);
            Assert.NotNull(user.Wallet);
            Assert.Equal(0m, user.Wallet.Balance.Amount); // Initial balance

            // Execute business logic
            var walletService = new WalletService();
            var transaction = walletService.Deposit(user, new Money(1000m, "USD"), "API Deposit");

            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            Assert.Equal(1000m, user.Wallet.Balance.Amount);

            // Simulate end of HTTP request
            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 3: Withdraw money (like POST /api/wallets/{id}/withdraw)
        // ============================================
        {
            // Load fresh from database again
            var user = _context.Users.Include(u => u.Wallet).FirstOrDefault(u => u.Id == userId);
            Assert.NotNull(user);
            Assert.NotNull(user.Wallet);
            Assert.Equal(1000m, user.Wallet.Balance.Amount); // Previous deposit persisted!

            // Execute business logic
            var walletService = new WalletService();
            var transaction = walletService.Withdraw(user, new Money(250m, "USD"), "API Withdrawal");

            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            Assert.Equal(750m, user.Wallet.Balance.Amount);

            // Simulate end of HTTP request
            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 4: Get balance (like GET /api/wallets/{id})
        // ============================================
        {
            // Load fresh from database
            var wallet = _context.Wallets.Find(walletId);
            Assert.NotNull(wallet);

            // Business logic still works on loaded entity
            Assert.Equal(750m, wallet.Balance.Amount);
            Assert.Equal(1000m, wallet.TotalDeposited.Amount);
            Assert.Equal(250m, wallet.TotalWithdrawn.Amount);

            // NetProfitLoss = TotalWon - TotalBet (no bets placed, so 0 - 0 = 0)
            Assert.Equal(0m, wallet.NetProfitLoss.Amount); // Computed property works!
        }
    }

    [Fact]
    public void StatelessAPI_CreateEvent_ThenLoadAndStart_ThenLoadAndComplete()
    {
        Guid eventId;

        // ============================================
        // REQUEST 1: Create event (like POST /api/events)
        // ============================================
        {
            var sport = new Sport("Baseball", "MLB");
            var league = new League("American League", "AL", sport.Id);
            var yankees = new Team("Yankees", "NYY", league.Id);
            var redsox = new Team("Red Sox", "BOS", league.Id);

            var game = new Event(
                "Yankees vs Red Sox",
                yankees,
                redsox,
                DateTime.UtcNow.AddHours(3),
                league.Id,
                "Yankee Stadium"
            );

            _context.Sports.Add(sport);
            _context.Leagues.Add(league);
            _context.Teams.AddRange(yankees, redsox);
            _context.Events.Add(game);
            _context.SaveChanges();

            eventId = game.Id;
            Assert.Equal(EventStatus.Scheduled, game.Status);

            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 2: Start event (like POST /api/events/{id}/start)
        // ============================================
        {
            // Load fresh from database
            var game = _context.Events.Find(eventId);
            Assert.NotNull(game);
            Assert.Equal(EventStatus.Scheduled, game.Status);

            // Execute business logic
            game.Start();
            _context.SaveChanges();

            Assert.Equal(EventStatus.InProgress, game.Status);

            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 3: Complete event (like POST /api/events/{id}/complete)
        // ============================================
        {
            // Load fresh from database
            var game = _context.Events.Find(eventId);
            Assert.NotNull(game);
            Assert.Equal(EventStatus.InProgress, game.Status);
            Assert.Null(game.FinalScore);

            // Execute business logic
            game.Complete(new Score(5, 3));
            _context.SaveChanges();

            Assert.Equal(EventStatus.Completed, game.Status);
            Assert.NotNull(game.FinalScore);
            Assert.Equal(5, game.FinalScore.Value.HomeScore);

            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 4: Get event details (like GET /api/events/{id})
        // ============================================
        {
            var game = _context.Events.Find(eventId);
            Assert.NotNull(game);

            // All state persisted correctly
            Assert.Equal(EventStatus.Completed, game.Status);
            Assert.Equal(5, game.FinalScore!.Value.HomeScore);
            Assert.Equal(3, game.FinalScore.Value.AwayScore);
        }
    }

    [Fact]
    public void StatelessAPI_CreateEventWithMarketAndOutcomes_ThenUpdateOdds()
    {
        Guid outcomeId;

        // ============================================
        // REQUEST 1: Create complete event with market and outcomes
        // ============================================
        {
            var sport = new Sport("Soccer", "SOC");
            var league = new League("MLS", "MLS", sport.Id);
            var team1 = new Team("Galaxy", "LAG", league.Id);
            var team2 = new Team("LAFC", "LAFC", league.Id);
            var game = new Event("Galaxy vs LAFC", team1, team2,
                DateTime.UtcNow.AddDays(1), league.Id, "Dignity Health");

            var market = new Market(MarketType.Moneyline, "Match Winner");
            game.AddMarket(market);

            var galaxyWin = new Outcome("Galaxy Win", "Galaxy wins", new Odds(2.20m));
            var draw = new Outcome("Draw", "Match draws", new Odds(3.30m));
            var lafcWin = new Outcome("LAFC Win", "LAFC wins", new Odds(2.80m));

            market.AddOutcome(galaxyWin);
            market.AddOutcome(draw);
            market.AddOutcome(lafcWin);

            _context.Sports.Add(sport);
            _context.Leagues.Add(league);
            _context.Teams.AddRange(team1, team2);
            _context.Events.Add(game);
            _context.SaveChanges();

            outcomeId = galaxyWin.Id;

            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 2: Update odds (like PUT /api/outcomes/{id}/odds)
        // ============================================
        {
            // Load outcome from database
            var outcome = _context.Outcomes.Find(outcomeId);
            Assert.NotNull(outcome);
            Assert.Equal(2.20m, outcome.CurrentOdds.DecimalValue);

            // Execute business logic - update odds
            outcome.UpdateOdds(new Odds(1.95m));
            _context.SaveChanges();

            Assert.Equal(1.95m, outcome.CurrentOdds.DecimalValue);

            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 3: Get updated odds (like GET /api/outcomes/{id})
        // ============================================
        {
            var outcome = _context.Outcomes.Find(outcomeId);
            Assert.NotNull(outcome);
            Assert.Equal(1.95m, outcome.CurrentOdds.DecimalValue); // Updated odds persisted!
        }
    }

    [Fact]
    public void StatelessAPI_PlaceBet_LoadAndVerify()
    {
        Guid userId;
        Guid betId;

        // ============================================
        // REQUEST 1: Setup user and event
        // ============================================
        {
            var user = new User("bettor", "bettor@test.com", "hash");
            var wallet = new Wallet(user);
            wallet.Deposit(new Money(500m, "USD"));

            var sport = new Sport("Basketball", "NBA");
            var league = new League("NBA", "NBA", sport.Id);
            var lakers = new Team("Lakers", "LAL", league.Id);
            var celtics = new Team("Celtics", "BOS", league.Id);
            var game = new Event("Lakers vs Celtics", lakers, celtics,
                DateTime.UtcNow.AddDays(1), league.Id, "Crypto.com");

            var market = new Market(MarketType.Moneyline, "Winner");
            game.AddMarket(market);

            var lakersWin = new Outcome("Lakers Win", "Lakers win", new Odds(1.75m));
            market.AddOutcome(lakersWin);

            _context.Users.Add(user);
            _context.Wallets.Add(wallet);
            _context.Sports.Add(sport);
            _context.Leagues.Add(league);
            _context.Teams.AddRange(lakers, celtics);
            _context.Events.Add(game);
            _context.SaveChanges();

            userId = user.Id;

            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 2: Place bet
        // ============================================
        {
            // Load user and event fresh
            var user = _context.Users.Include(u => u.Wallet).First(u => u.Id == userId);
            var game = _context.Events
                .Include(e => e.Markets)
                    .ThenInclude(m => m.Outcomes)
                .First();

            var market = game.Markets.First();
            var outcome = market.Outcomes.First();

            Assert.Equal(500m, user.Wallet.Balance.Amount);

            // Execute business logic
            var bet = Bet.CreateSingle(user, new Money(100m, "USD"), game, market, outcome);
            var walletService = new WalletService();
            var transaction = walletService.PlaceBet(user, bet);

            _context.Bets.Add(bet);
            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            betId = bet.Id;
            Assert.Equal(400m, user.Wallet.Balance.Amount); // Bet deducted!

            _context.ChangeTracker.Clear();
        }

        // ============================================
        // REQUEST 3: Get bet details
        // ============================================
        {
            // Load bet fresh from database
            var bet = _context.Bets
                .Include(b => b.Selections)
                .FirstOrDefault(b => b.Id == betId);

            Assert.NotNull(bet);
            Assert.Equal(BetType.Single, bet.Type);
            Assert.Equal(BetStatus.Pending, bet.Status);
            Assert.Equal(100m, bet.Stake.Amount);
            Assert.Equal(1.75m, bet.CombinedOdds.DecimalValue);
            Assert.Equal(175m, bet.PotentialPayout.Amount); // Computed correctly!
            Assert.Single(bet.Selections);

            // Business logic methods work on loaded entity
            Assert.False(bet.IsSettled);
        }

        // ============================================
        // REQUEST 4: Get user's wallet after bet
        // ============================================
        {
            var wallet = _context.Wallets.FirstOrDefault(w => w.UserId == userId);

            Assert.NotNull(wallet);
            Assert.Equal(400m, wallet.Balance.Amount);
            Assert.Equal(100m, wallet.TotalBet.Amount);
            Assert.Equal(500m, wallet.TotalDeposited.Amount);
        }
    }

    [Fact]
    public void StatelessAPI_ValueObjectsWorkAfterRoundTrip()
    {
        Guid walletId;

        // ============================================
        // Save some Money value objects
        // ============================================
        {
            var user = new User("vouser", "vo@test.com", "hash", "EUR");
            var wallet = new Wallet(user);
            wallet.Deposit(new Money(1234.56m, "EUR"));
            wallet.Withdraw(new Money(234.56m, "EUR"));

            _context.Users.Add(user);
            _context.Wallets.Add(wallet);
            _context.SaveChanges();

            walletId = wallet.Id;

            _context.ChangeTracker.Clear();
        }

        // ============================================
        // Load and verify value objects reconstruct correctly
        // ============================================
        {
            var wallet = _context.Wallets.Find(walletId);
            Assert.NotNull(wallet);

            // Money value objects work correctly
            Assert.Equal(1000m, wallet.Balance.Amount);
            Assert.Equal("EUR", wallet.Balance.Currency);

            // Can use Money in business logic (operator overloading)
            var newBalance = wallet.Balance + new Money(500m, "EUR");
            Assert.Equal(1500m, newBalance.Amount);

            // Money equality works
            var expected = new Money(1000m, "EUR");
            Assert.Equal(expected, wallet.Balance);

            // Money ToString works
            Assert.Contains("1000", wallet.Balance.ToString());
            Assert.Contains("EUR", wallet.Balance.ToString());
        }
    }

    [Fact]
    public void StatelessAPI_NavigationPropertiesLoadCorrectly()
    {
        Guid eventId;

        // ============================================
        // Create complex object graph
        // ============================================
        {
            var sport = new Sport("Hockey", "NHL");
            var league = new League("NHL", "NHL", sport.Id);
            var bruins = new Team("Bruins", "BOS", league.Id);
            var leafs = new Team("Leafs", "TOR", league.Id);

            var game = new Event("Bruins vs Leafs", bruins, leafs,
                DateTime.UtcNow.AddDays(1), league.Id, "TD Garden");

            var moneyline = new Market(MarketType.Moneyline, "Winner");
            var overUnder = new Market(MarketType.Totals, "Total Goals");

            game.AddMarket(moneyline);
            game.AddMarket(overUnder);

            moneyline.AddOutcome(new Outcome("Bruins Win", "Bruins win", new Odds(1.90m)));
            moneyline.AddOutcome(new Outcome("Leafs Win", "Leafs win", new Odds(2.10m)));

            overUnder.AddOutcome(new Outcome("Over 5.5", "Over 5.5 goals", new Odds(1.85m)));
            overUnder.AddOutcome(new Outcome("Under 5.5", "Under 5.5 goals", new Odds(1.95m)));

            _context.Sports.Add(sport);
            _context.Leagues.Add(league);
            _context.Teams.AddRange(bruins, leafs);
            _context.Events.Add(game);
            _context.SaveChanges();

            eventId = game.Id;

            _context.ChangeTracker.Clear();
        }

        // ============================================
        // Load with navigation properties
        // ============================================
        {
            var game = _context.Events
                .Include(e => e.Markets)
                    .ThenInclude(m => m.Outcomes)
                .FirstOrDefault(e => e.Id == eventId);

            Assert.NotNull(game);
            Assert.Equal(2, game.Markets.Count);

            var moneyline = game.Markets.First(m => m.Type == MarketType.Moneyline);
            Assert.Equal(2, moneyline.Outcomes.Count);

            var overUnder = game.Markets.First(m => m.Type == MarketType.Totals);
            Assert.Equal(2, overUnder.Outcomes.Count);

            // Business logic works on loaded collections
            var bruinsWin = moneyline.Outcomes.First(o => o.Name == "Bruins Win");
            Assert.Equal(1.90m, bruinsWin.CurrentOdds.DecimalValue);
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
