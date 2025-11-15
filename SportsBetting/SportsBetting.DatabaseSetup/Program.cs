using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

Console.WriteLine("SportsBetting Database Creator");
Console.WriteLine("================================\n");

// Connection string for local PostgreSQL (Homebrew installation)
var connectionString = Environment.GetEnvironmentVariable("SPORTSBETTING_DB")
    ?? "Host=localhost;Database=sportsbetting;Username=calebwilliams";

Console.WriteLine("Connection string: " + connectionString);
Console.WriteLine("\nCreating DbContext...");

var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
    .UseNpgsql(connectionString)
    .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
    .Options;

var context = new SportsBettingDbContext(options);

Console.WriteLine("\nApplying migrations...");
context.Database.Migrate();
Console.WriteLine("✓ Database schema created!");

Console.WriteLine("\nCreating sample data...");

// Create a user
var user = new User("demo_user", "demo@sportsbetting.com", "hashed_password_123");
var wallet = new Wallet(user);
var walletService = new WalletService();

// Deposit money
var depositTx = walletService.Deposit(user, new Money(5000m, "USD"), "Initial deposit");

// Create a sport
var soccer = new Sport("Soccer", "SOC");
var league = new League("English Premier League", "EPL", soccer.Id);
var arsenal = new Team("Arsenal", "ARS", league.Id);
var chelsea = new Team("Chelsea", "CHE", league.Id);

// Create an event
var match = new Event(
    "Arsenal vs Chelsea",
    arsenal,
    chelsea,
    DateTime.UtcNow.AddDays(2),
    league.Id,
    "Emirates Stadium"
);

// Create a market
var moneylineMarket = new Market(MarketType.Moneyline, "Match Winner", "Pick the winning team");
match.AddMarket(moneylineMarket);

// Create outcomes
var arsenalWin = new Outcome("Arsenal Win", "Arsenal wins the match", new Odds(2.10m));
var draw = new Outcome("Draw", "Match ends in a draw", new Odds(3.40m));
var chelseaWin = new Outcome("Chelsea Win", "Chelsea wins the match", new Odds(3.75m));

moneylineMarket.AddOutcome(arsenalWin);
moneylineMarket.AddOutcome(draw);
moneylineMarket.AddOutcome(chelseaWin);

// Place a bet
var bet = Bet.CreateSingle(user, new Money(100m, "USD"), match, moneylineMarket, arsenalWin);
var betTx = walletService.PlaceBet(user, bet);

// Save everything
context.Users.Add(user);
context.Wallets.Add(wallet);
context.Transactions.AddRange(depositTx, betTx);
context.Sports.Add(soccer);
context.Leagues.Add(league);
context.Teams.AddRange(arsenal, chelsea);
context.Events.Add(match);
context.Bets.Add(bet);

context.SaveChanges();

Console.WriteLine("✓ Sample data created!");
Console.WriteLine("\n=== Summary ===");
Console.WriteLine($"User: {user.Username}");
Console.WriteLine($"Wallet Balance: {wallet.Balance}");
Console.WriteLine($"Transactions: {context.Transactions.Count()}");
Console.WriteLine($"Sports: {context.Sports.Count()}");
Console.WriteLine($"Leagues: {context.Leagues.Count()}");
Console.WriteLine($"Teams: {context.Teams.Count()}");
Console.WriteLine($"Events: {context.Events.Count()}");
Console.WriteLine($"Markets: {context.Markets.Count()}");
Console.WriteLine($"Outcomes: {context.Outcomes.Count()}");
Console.WriteLine($"Bets: {context.Bets.Count()}");

Console.WriteLine("\n✓ Database ready! Check pgAdmin to see all tables and data.");
