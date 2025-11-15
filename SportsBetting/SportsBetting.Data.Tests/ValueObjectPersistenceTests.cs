using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Tests;

public class ValueObjectPersistenceTests : IDisposable
{
    private readonly SportsBettingDbContext _context;

    public ValueObjectPersistenceTests()
    {
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new SportsBettingDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void MoneyValueObjectsArePersisted()
    {
        // Arrange
        var user = new User("moneytest", "money@test.com", "pass");
        var wallet = new Wallet(user);
        wallet.Deposit(new Money(250.75m, "USD"));
        wallet.Withdraw(new Money(50.25m, "USD"));

        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act
        _context.Entry(wallet).State = EntityState.Detached;
        var retrieved = _context.Wallets.Find(wallet.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(200.50m, retrieved.Balance.Amount);
        Assert.Equal("USD", retrieved.Balance.Currency);
        Assert.Equal(250.75m, retrieved.TotalDeposited.Amount);
        Assert.Equal(50.25m, retrieved.TotalWithdrawn.Amount);
    }

    [Fact]
    public void OddsValueObjectsArePersisted()
    {
        // Arrange
        var sport = new Sport("Football", "FB");
        var league = new League("NFL", "NFL", sport.Id);
        var team1 = new Team("Patriots", "PAT", league.Id);
        var team2 = new Team("Chiefs", "KC", league.Id);
        var gameEvent = new Event("Patriots vs Chiefs", team1, team2,
            DateTime.UtcNow.AddDays(1), league.Id, "Gillette Stadium");
        var market = new Market(MarketType.Moneyline, "Moneyline");
        gameEvent.AddMarket(market);
        var outcome = new Outcome("Patriots Win", "Patriots to win the game", new Odds(2.5m));
        market.AddOutcome(outcome);

        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(team1, team2);
        _context.Events.Add(gameEvent);
        _context.SaveChanges();

        // Act
        _context.Entry(outcome).State = EntityState.Detached;
        var retrieved = _context.Outcomes.Find(outcome.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(2.5m, retrieved.CurrentOdds.DecimalValue);
    }

    [Fact]
    public void NullableScoreValueObjectIsPersisted()
    {
        // Arrange
        var sport = new Sport("Basketball", "BB");
        var league = new League("NBA", "NBA", sport.Id);
        var team1 = new Team("Lakers", "LAL", league.Id);
        var team2 = new Team("Celtics", "BOS", league.Id);
        var gameEvent = new Event("Lakers vs Celtics", team1, team2,
            DateTime.UtcNow.AddDays(1), league.Id, "Staples Center");

        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(team1, team2);
        _context.Events.Add(gameEvent);
        _context.SaveChanges();

        // Act - Set final score using Start and Complete
        gameEvent.Start();
        gameEvent.Complete(new Score(110, 105));
        _context.SaveChanges();

        _context.Entry(gameEvent).State = EntityState.Detached;
        var retrieved = _context.Events.Find(gameEvent.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.FinalScore);
        Assert.Equal(110, retrieved.FinalScore.Value.HomeScore);
        Assert.Equal(105, retrieved.FinalScore.Value.AwayScore);
    }

    [Fact]
    public void ComplexPropertyCurrencyIsPersisted()
    {
        // Arrange
        var user = new User("currencytest", "currency@test.com", "pass", "EUR");
        var wallet = new Wallet(user);
        wallet.Deposit(new Money(100m, "EUR"));

        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act
        _context.Entry(wallet).State = EntityState.Detached;
        var retrieved = _context.Wallets.Find(wallet.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("EUR", retrieved.Balance.Currency);
        Assert.Equal("EUR", retrieved.TotalDeposited.Currency);
    }

    [Fact]
    public void BetWithComplexOddsIsPersisted()
    {
        // Arrange - Create full betting scenario
        var user = new User("betuser", "bet@test.com", "pass");
        var wallet = new Wallet(user);
        wallet.Deposit(new Money(500m, "USD"));

        var sport = new Sport("Soccer", "SC");
        var league = new League("EPL", "EPL", sport.Id);
        var team1 = new Team("Arsenal", "ARS", league.Id);
        var team2 = new Team("Chelsea", "CHE", league.Id);
        var gameEvent = new Event("Arsenal vs Chelsea", team1, team2,
            DateTime.UtcNow.AddDays(1), league.Id, "Emirates Stadium");
        var market = new Market(MarketType.Moneyline, "Match Winner");
        gameEvent.AddMarket(market);
        var outcome = new Outcome("Arsenal Win", "Arsenal wins", new Odds(1.85m));
        market.AddOutcome(outcome);

        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(team1, team2);
        _context.Events.Add(gameEvent);
        _context.SaveChanges();

        // Create bet
        var bet = Bet.CreateSingle(user, new Money(100m, "USD"), gameEvent, market, outcome);
        _context.Bets.Add(bet);
        _context.SaveChanges();

        // Act
        _context.Entry(bet).State = EntityState.Detached;
        var retrieved = _context.Bets
            .Include(b => b.Selections)
            .FirstOrDefault(b => b.Id == bet.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(1.85m, retrieved.CombinedOdds.DecimalValue);
        Assert.Equal(100m, retrieved.Stake.Amount);
        Assert.Equal("USD", retrieved.Stake.Currency);
        Assert.Equal(185m, retrieved.PotentialPayout.Amount);
        Assert.Single(retrieved.Selections);
        Assert.Equal(1.85m, retrieved.Selections[0].LockedOdds.DecimalValue);
    }

    [Fact]
    public void NullableMoneyIsPersistedCorrectly()
    {
        // Arrange
        var user = new User("payoutuser", "payout@test.com", "pass");
        var wallet = new Wallet(user);
        wallet.Deposit(new Money(100m, "USD"));

        var sport = new Sport("Tennis", "TN");
        var league = new League("ATP", "ATP", sport.Id);
        var team1 = new Team("Federer", "FED", league.Id);
        var team2 = new Team("Nadal", "NAD", league.Id);
        var gameEvent = new Event("Federer vs Nadal", team1, team2,
            DateTime.UtcNow.AddDays(1), league.Id, "Wimbledon");
        var market = new Market(MarketType.Moneyline, "Winner");
        gameEvent.AddMarket(market);
        var outcome = new Outcome("Federer Win", "Federer wins", new Odds(2.0m));
        market.AddOutcome(outcome);

        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.Sports.Add(sport);
        _context.Leagues.Add(league);
        _context.Teams.AddRange(team1, team2);
        _context.Events.Add(gameEvent);

        var bet = Bet.CreateSingle(user, new Money(50m, "USD"), gameEvent, market, outcome);
        _context.Bets.Add(bet);
        _context.SaveChanges();

        // Act - Before settlement, ActualPayout is null
        var betBeforeSettlement = _context.Bets.Find(bet.Id);
        Assert.NotNull(betBeforeSettlement);
        Assert.Null(betBeforeSettlement.ActualPayout);

        // Verify null Money is persisted correctly
        _context.Entry(bet).State = EntityState.Detached;
        var betRetrieved = _context.Bets.Find(bet.Id);
        Assert.NotNull(betRetrieved);
        Assert.Null(betRetrieved.ActualPayout); // Nullable Money persisted as null
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
