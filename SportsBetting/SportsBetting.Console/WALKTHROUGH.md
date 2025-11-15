# Sports Betting Domain Model - User Walkthrough

This guide walks you through creating and using all the core entities in the Sports Betting domain model.

## Prerequisites

```bash
cd SportsBetting
dotnet build
```

## Table of Contents

1. [Creating the Sports Hierarchy](#1-creating-the-sports-hierarchy)
2. [Creating Events and Markets](#2-creating-events-and-markets)
3. [Placing Single Bets](#3-placing-single-bets)
4. [Placing Parlay Bets](#4-placing-parlay-bets)
5. [Settling Bets](#5-settling-bets)
6. [Using LineLock (Odds Options)](#6-using-linelock-odds-options)
7. [Handling Cancellations](#7-handling-cancellations)
8. [Advanced Scenarios](#8-advanced-scenarios)

---

## 1. Creating the Sports Hierarchy

### Step 1.1: Create a Sport

```csharp
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.ValueObjects;
using SportsBetting.Domain.Enums;

// Create a sport
var football = new Sport("American Football", "NFL");
Console.WriteLine($"Created Sport: {football.Name} ({football.Code})");
```

**Output:**
```
Created Sport: American Football (NFL)
```

### Step 1.2: Create a League

```csharp
// Create a league within the sport
var nfl = new League("National Football League", "NFL", football.Id);
football.AddLeague(nfl);

Console.WriteLine($"Created League: {nfl.Name}");
```

### Step 1.3: Create Teams

```csharp
// Create teams in the league
var chiefs = new Team("Chiefs", "KC", nfl.Id, "Kansas City");
var ravens = new Team("Ravens", "BAL", nfl.Id, "Baltimore");

nfl.AddTeam(chiefs);
nfl.AddTeam(ravens);

Console.WriteLine($"Created Teams: {chiefs.FullName} and {ravens.FullName}");
```

**Why this hierarchy?**
- Sport → League → Teams follows real-world organization
- IDs link entities together (Team.LeagueId, League.SportId)
- Allows filtering by sport/league later

---

## 2. Creating Events and Markets

### Step 2.1: Create an Event (Game/Match)

```csharp
// Create an upcoming event
var game = new Event(
    name: "Chiefs vs Ravens - Week 12",
    homeTeam: chiefs,
    awayTeam: ravens,
    scheduledStartTime: DateTime.UtcNow.AddHours(2),
    leagueId: nfl.Id,
    venue: "Arrowhead Stadium"
);

Console.WriteLine($"Created Event: {game.Name}");
Console.WriteLine($"  Start Time: {game.ScheduledStartTime}");
Console.WriteLine($"  Status: {game.Status}");
```

### Step 2.2: Create Markets

#### Moneyline Market (Who Wins)

```csharp
var moneyline = new Market(MarketType.Moneyline, "Match Winner");

// Add outcomes with odds
moneyline.AddOutcome(new Outcome(
    name: "Chiefs Win",
    description: "Kansas City Chiefs to win",
    initialOdds: new Odds(1.90m)  // Decimal odds
));

moneyline.AddOutcome(new Outcome(
    name: "Ravens Win",
    description: "Baltimore Ravens to win",
    initialOdds: new Odds(2.05m)
));

// Attach market to event
game.AddMarket(moneyline);

Console.WriteLine($"Created Market: {moneyline.Name}");
foreach (var outcome in moneyline.Outcomes)
{
    Console.WriteLine($"  - {outcome.Name} @ {outcome.CurrentOdds}");
}
```

**Output:**
```
Created Market: Match Winner
  - Chiefs Win @ 1.90
  - Ravens Win @ 2.05
```

#### Point Spread Market

```csharp
var spread = new Market(MarketType.Spread, "Point Spread");

spread.AddOutcome(new Outcome(
    name: "Chiefs -6.5",
    description: "Chiefs to cover -6.5",
    initialOdds: new Odds(1.91m),
    line: -6.5m  // The spread line
));

spread.AddOutcome(new Outcome(
    name: "Ravens +6.5",
    description: "Ravens to cover +6.5",
    initialOdds: new Odds(1.91m),
    line: +6.5m
));

game.AddMarket(spread);
```

#### Totals (Over/Under) Market

```csharp
var totals = new Market(MarketType.Totals, "Total Points");

totals.AddOutcome(new Outcome(
    name: "Over 47.5",
    description: "Total points over 47.5",
    initialOdds: new Odds(1.87m),
    line: 47.5m
));

totals.AddOutcome(new Outcome(
    name: "Under 47.5",
    description: "Total points under 47.5",
    initialOdds: new Odds(1.95m),
    line: 47.5m
));

game.AddMarket(totals);
```

---

## 3. Placing Single Bets

### Step 3.1: Create a Money Value

```csharp
// Create a stake amount
var stake = new Money(100m, "USD");
Console.WriteLine($"Stake: {stake}");  // Output: 100.00 USD
```

**Money Features:**
- Type-safe (no raw decimals)
- Currency validation
- Negative amounts rejected
- Math operations (addition, subtraction, multiplication)

### Step 3.2: Place a Single Bet

```csharp
// Get the outcome you want to bet on
var chiefsOutcome = moneyline.Outcomes.First(o => o.Name.Contains("Chiefs"));

// Create the bet
var bet = Bet.CreateSingle(
    stake: stake,
    evt: game,
    market: moneyline,
    outcome: chiefsOutcome
);

Console.WriteLine($"✓ Bet Placed: {bet.TicketNumber}");
Console.WriteLine($"  Selection: {bet.Selections[0]}");
Console.WriteLine($"  Stake: {bet.Stake}");
Console.WriteLine($"  Odds: {bet.CombinedOdds}");
Console.WriteLine($"  Potential Payout: {bet.PotentialPayout}");
Console.WriteLine($"  Potential Profit: {bet.PotentialProfit}");
Console.WriteLine($"  Status: {bet.Status}");
```

**Output:**
```
✓ Bet Placed: BET20251114180612345
  Selection: Chiefs vs Ravens - Chiefs Win @ 1.90
  Stake: 100.00 USD
  Odds: 1.90
  Potential Payout: 190.00 USD
  Potential Profit: 90.00 USD
  Status: Pending
```

### Step 3.3: Understanding Odds

```csharp
// Decimal odds examples
var evenOdds = new Odds(2.0m);        // Even money (100% profit)
var favoriteOdds = new Odds(1.50m);   // 50% profit
var underdogOdds = new Odds(3.00m);   // 200% profit

// Convert to American odds
var americanFavorite = Odds.FromAmerican(-150);  // = 1.67 decimal
var americanUnderdog = Odds.FromAmerican(+200);  // = 3.00 decimal

Console.WriteLine($"American -150 = Decimal {americanFavorite.DecimalValue}");
Console.WriteLine($"Decimal 1.67 = American {americanFavorite.ToAmerican()}");

// Calculate payouts
var myStake = new Money(50m, "USD");
var myOdds = new Odds(2.50m);

var payout = myOdds.CalculatePayout(myStake);  // 50 × 2.50 = 125
var profit = myOdds.CalculateProfit(myStake);  // 50 × (2.50 - 1) = 75

Console.WriteLine($"Stake: {myStake}");
Console.WriteLine($"Payout: {payout}");  // 125.00 USD
Console.WriteLine($"Profit: {profit}");   // 75.00 USD
```

---

## 4. Placing Parlay Bets

### Step 4.1: Create Multiple Games

```csharp
// Create 3 different games
var game1 = new Event("Lakers vs Celtics", lakers, celtics, DateTime.UtcNow.AddHours(2), nba.Id);
var game2 = new Event("Warriors vs Nets", warriors, nets, DateTime.UtcNow.AddHours(3), nba.Id);
var game3 = new Event("Heat vs Bulls", heat, bulls, DateTime.UtcNow.AddHours(4), nba.Id);

// Create markets for each game
var ml1 = new Market(MarketType.Moneyline, "Match Winner");
ml1.AddOutcome(new Outcome("Lakers Win", "Lakers", new Odds(1.80m)));
game1.AddMarket(ml1);

var ml2 = new Market(MarketType.Moneyline, "Match Winner");
ml2.AddOutcome(new Outcome("Warriors Win", "Warriors", new Odds(1.65m)));
game2.AddMarket(ml2);

var ml3 = new Market(MarketType.Moneyline, "Match Winner");
ml3.AddOutcome(new Outcome("Heat Win", "Heat", new Odds(1.95m)));
game3.AddMarket(ml3);
```

### Step 4.2: Place a Parlay Bet

```csharp
// Create parlay with 3 legs
var parlay = Bet.CreateParlay(
    stake: new Money(50m, "USD"),
    (game1, ml1, ml1.Outcomes.First(o => o.Name.Contains("Lakers"))),
    (game2, ml2, ml2.Outcomes.First(o => o.Name.Contains("Warriors"))),
    (game3, ml3, ml3.Outcomes.First(o => o.Name.Contains("Heat")))
);

Console.WriteLine($"✓ Parlay Bet Placed: {parlay.TicketNumber}");
Console.WriteLine($"  Type: {parlay.Type}");
Console.WriteLine($"  Legs: {parlay.Selections.Count}");
Console.WriteLine($"  Stake: {parlay.Stake}");
Console.WriteLine($"  Combined Odds: {parlay.CombinedOdds.DecimalValue:F2}");
Console.WriteLine($"    (1.80 × 1.65 × 1.95 = {1.80m * 1.65m * 1.95m:F2})");
Console.WriteLine($"  Potential Payout: {parlay.PotentialPayout}");
Console.WriteLine($"  Potential Profit: {parlay.PotentialProfit}");

Console.WriteLine($"\n  Selections:");
foreach (var selection in parlay.Selections)
{
    Console.WriteLine($"    - {selection}");
}
```

**Output:**
```
✓ Parlay Bet Placed: BET20251114180612789
  Type: Parlay
  Legs: 3
  Stake: 50.00 USD
  Combined Odds: 5.79
    (1.80 × 1.65 × 1.95 = 5.79)
  Potential Payout: 289.58 USD
  Potential Profit: 239.58 USD

  Selections:
    - Lakers vs Celtics - Lakers Win @ 1.80
    - Warriors vs Nets - Warriors Win @ 1.65
    - Heat vs Bulls - Heat Win @ 1.95
```

**Parlay Rules:**
- Minimum 2 legs (enforced)
- All legs must win for parlay to win
- One loss = entire parlay loses
- Void legs are removed, odds recalculated

---

## 5. Settling Bets

### Step 5.1: Complete the Event

```csharp
// Game finishes
var finalScore = new Score(homeScore: 28, awayScore: 24);
game.Complete(finalScore);

Console.WriteLine($"Game completed: {game.Status}");
Console.WriteLine($"Final Score: {game.HomeTeam.Name} {finalScore.HomeScore} - {finalScore.AwayScore} {game.AwayTeam.Name}");
Console.WriteLine($"  Winner: {finalScore.IsHomeWin ? game.HomeTeam.Name : game.AwayTeam.Name}");
Console.WriteLine($"  Margin: {finalScore.Margin}");
Console.WriteLine($"  Total Points: {finalScore.TotalPoints}");
```

### Step 5.2: Settle Markets

```csharp
using SportsBetting.Domain.Services;

var settlementService = new SettlementService();

// Settle moneyline market
settlementService.SettleMoneylineMarket(moneyline, game);

Console.WriteLine($"Market settled: {moneyline.Name}");
foreach (var outcome in moneyline.Outcomes)
{
    Console.WriteLine($"  {outcome.Name}: {(outcome.IsWinner == true ? "WON" : "LOST")}");
}
```

**Output:**
```
Market settled: Match Winner
  Chiefs Win: WON
  Ravens Win: LOST
```

### Step 5.3: Settle Bets

```csharp
// Settle the bet
settlementService.SettleBet(bet, new[] { game });

Console.WriteLine($"\n✓ Bet Settled");
Console.WriteLine($"  Status: {bet.Status}");
Console.WriteLine($"  Actual Payout: {bet.ActualPayout}");
Console.WriteLine($"  Profit: {bet.ActualProfit}");
```

**Output:**
```
✓ Bet Settled
  Status: Won
  Actual Payout: 190.00 USD
  Profit: 90.00 USD
```

### Step 5.4: Spread Settlement (with Push)

```csharp
// Team wins by EXACTLY the spread
var spreadGame = new Event("Packers vs Bears", packers, bears, DateTime.UtcNow, nfl.Id);
var spreadMarket = new Market(MarketType.Spread, "Point Spread");

spreadMarket.AddOutcome(new Outcome("Packers -7", "Packers -7", new Odds(1.91m), -7m));
spreadMarket.AddOutcome(new Outcome("Bears +7", "Bears +7", new Odds(1.91m), +7m));
spreadGame.AddMarket(spreadMarket);

var spreadBet = Bet.CreateSingle(
    new Money(100m),
    spreadGame,
    spreadMarket,
    spreadMarket.Outcomes.First()
);

// Packers win by exactly 7 → PUSH
spreadGame.Complete(new Score(21, 14));
settlementService.SettleSpreadMarket(spreadMarket, spreadGame);
settlementService.SettleBet(spreadBet, new[] { spreadGame });

Console.WriteLine($"Spread Bet Status: {spreadBet.Status}");        // Pushed
Console.WriteLine($"Refund: {spreadBet.ActualPayout}");             // 100.00 USD
Console.WriteLine($"Profit: {spreadBet.ActualProfit}");             // 0.00 USD
```

### Step 5.5: Totals Settlement

```csharp
settlementService.SettleTotalsMarket(totals, game);

// If total = 55, line = 47.5 → Over wins
// If total = 45, line = 47.5 → Under wins
// If total = 47.5 exactly → Push (rare, half-point lines prevent this)
```

---

## 6. Using LineLock (Odds Options)

LineLock allows you to **lock in current odds** by paying a premium, protecting against odds movements.

### Step 6.1: Create a LineLock

```csharp
var futureGame = new Event("Ravens vs Steelers", ravens, steelers, DateTime.UtcNow.AddHours(3), nfl.Id);
var futureMarket = new Market(MarketType.Moneyline, "Match Winner");
var ravensOutcome = new Outcome("Ravens Win", "Ravens", new Odds(2.20m));
futureMarket.AddOutcome(ravensOutcome);
futureGame.AddMarket(futureMarket);

// Buy a LineLock
var lineLock = LineLock.Create(
    evt: futureGame,
    market: futureMarket,
    outcome: ravensOutcome,
    lockFee: new Money(5m, "USD"),        // Premium you pay
    maxStake: new Money(200m, "USD"),     // Maximum bet size covered
    expirationTime: DateTime.UtcNow.AddMinutes(30)
);

Console.WriteLine($"✓ LineLock Created: {lineLock.LockNumber}");
Console.WriteLine($"  Locked Odds: {lineLock.LockedOdds}");
Console.WriteLine($"  Lock Fee: {lineLock.LockFee}");
Console.WriteLine($"  Max Stake: {lineLock.MaxStake}");
Console.WriteLine($"  Expires: {lineLock.ExpirationTime}");
Console.WriteLine($"  Status: {lineLock.Status}");
Console.WriteLine($"  Can Exercise: {lineLock.CanExercise}");
```

### Step 6.2: Odds Move (Market Changes)

```csharp
// Breaking news: Injury to key player
// Market reacts, odds drop to 1.85

ravensOutcome.UpdateOdds(new Odds(1.85m));

Console.WriteLine($"\n📉 Odds Dropped!");
Console.WriteLine($"  Market Odds NOW: {ravensOutcome.CurrentOdds}");
Console.WriteLine($"  Your LineLock STILL: {lineLock.LockedOdds}");
Console.WriteLine($"  Value Saved: {(lineLock.LockedOdds.DecimalValue - ravensOutcome.CurrentOdds.DecimalValue):F2} odds points");
```

### Step 6.3: Exercise the LineLock

```csharp
// Exercise your lock with desired stake (≤ maxStake)
var exerciseStake = new Money(150m, "USD");
var lockedBet = lineLock.Exercise(exerciseStake, futureGame, futureMarket, ravensOutcome);

Console.WriteLine($"\n✓ LineLock Exercised");
Console.WriteLine($"  Bet Created: {lockedBet.TicketNumber}");
Console.WriteLine($"  Stake: {lockedBet.Stake}");
Console.WriteLine($"  Odds Used: {lockedBet.CombinedOdds}");
Console.WriteLine($"  Current Market Odds: {ravensOutcome.CurrentOdds}");
Console.WriteLine($"  Was LineLocked: {lockedBet.WasLineLocked}");
Console.WriteLine($"  LineLock Status: {lineLock.Status}");

// Calculate value gained
var currentPayout = ravensOutcome.CurrentOdds.CalculatePayout(exerciseStake);
var lockedPayout = lockedBet.PotentialPayout;
var valueGained = lockedPayout.Amount - currentPayout.Amount;

Console.WriteLine($"\n💰 Value Analysis:");
Console.WriteLine($"  Payout at current odds (1.85): {currentPayout}");
Console.WriteLine($"  Payout at locked odds (2.20): {lockedPayout}");
Console.WriteLine($"  Extra value: ${valueGained}");
Console.WriteLine($"  Less lock fee: ${valueGained - lineLock.LockFee.Amount}");
```

### Step 6.4: LineLock Expiration

```csharp
// If you don't exercise before expiration
if (DateTime.UtcNow > lineLock.ExpirationTime)
{
    lineLock.Expire();
    Console.WriteLine($"LineLock Status: {lineLock.Status}");  // Expired
    Console.WriteLine($"Lock fee is lost.");
}
```

### Step 6.5: Multiple Locks on Same Outcome

```csharp
// You can buy multiple locks with different parameters
var lock1 = LineLock.Create(futureGame, futureMarket, ravensOutcome,
    new Money(5m), new Money(100m), DateTime.UtcNow.AddMinutes(30));

var lock2 = LineLock.Create(futureGame, futureMarket, ravensOutcome,
    new Money(3m), new Money(50m), DateTime.UtcNow.AddMinutes(20));

Console.WriteLine($"Lock 1: Max ${lock1.MaxStake.Amount}, Fee ${lock1.LockFee.Amount}");
Console.WriteLine($"Lock 2: Max ${lock2.MaxStake.Amount}, Fee ${lock2.LockFee.Amount}");
```

---

## 7. Handling Cancellations

### Step 7.1: Event Cancellation

```csharp
var scheduledGame = new Event("Cowboys vs Eagles", cowboys, eagles,
    DateTime.UtcNow.AddHours(2), nfl.Id);
var market = new Market(MarketType.Moneyline, "Winner");
market.AddOutcome(new Outcome("Cowboys Win", "Cowboys", new Odds(1.95m)));
scheduledGame.AddMarket(market);

// Place bet
var bet = Bet.CreateSingle(new Money(100m), scheduledGame, market, market.Outcomes[0]);
Console.WriteLine($"Bet placed: {bet.TicketNumber}");

// Event gets cancelled (weather, etc.)
scheduledGame.Cancel();
market.SettleAsVoid();

Console.WriteLine($"\nEvent Status: {scheduledGame.Status}");  // Cancelled
Console.WriteLine($"Market Status: {market.IsSettled}");       // True
```

### Step 7.2: Bet Refund

```csharp
settlementService.SettleBet(bet, new[] { scheduledGame });

Console.WriteLine($"\nBet Status: {bet.Status}");              // Void
Console.WriteLine($"Refund Amount: {bet.ActualPayout}");       // 100.00 USD (full stake)
Console.WriteLine($"Net to Customer: ${bet.ActualPayout.Value.Amount}");
```

### Step 7.3: LineLock Cancellation

```csharp
// Cancel LineLock when event cancelled
lineLock.Cancel();

Console.WriteLine($"LineLock Status: {lineLock.Status}");      // Cancelled
Console.WriteLine($"Note: Lock fee refund handled by wallet service");
```

### Step 7.4: Manual Void (Customer Service)

```csharp
// Manual void for exceptional circumstances
bet.Void();

Console.WriteLine($"Bet manually voided");
Console.WriteLine($"Status: {bet.Status}");                    // Void
Console.WriteLine($"Refund: {bet.ActualPayout}");              // Full stake
```

---

## 8. Advanced Scenarios

### Scenario 8.1: Parlay with Void Leg

```csharp
// 2-leg parlay
var parlay = Bet.CreateParlay(
    new Money(100m),
    (game1, ml1, ml1.Outcomes[0]),  // Odds: 2.00
    (game2, ml2, ml2.Outcomes[0])   // Odds: 1.75
);

Console.WriteLine($"Original Combined Odds: {parlay.CombinedOdds.DecimalValue:F2}");
// 2.00 × 1.75 = 3.50

// Game 1: Team wins
game1.Complete(new Score(5, 3));
settlementService.SettleMoneylineMarket(ml1, game1);

// Game 2: Cancelled
game2.Cancel();
ml2.SettleAsVoid();

// Settle parlay
settlementService.SettleBet(parlay, new[] { game1, game2 });

Console.WriteLine($"\nParlay Status: {parlay.Status}");        // Won
Console.WriteLine($"Recalculated Odds: 2.00 (only game1)");
Console.WriteLine($"Payout: {parlay.ActualPayout}");           // 200.00 USD
Console.WriteLine($"Explanation: Void leg removed, parlay reduced to single bet");
```

### Scenario 8.2: Same-Game Parlay

```csharp
// Some sportsbooks allow parlays on same game with special handling
var game = new Event("Patriots vs Jets", patriots, jets, DateTime.UtcNow, nfl.Id);

var ml = new Market(MarketType.Moneyline, "Winner");
ml.AddOutcome(new Outcome("Patriots Win", "Patriots", new Odds(1.50m)));

var totals = new Market(MarketType.Totals, "Total Points");
totals.AddOutcome(new Outcome("Over 45", "Over", new Odds(1.90m), 45m));

game.AddMarket(ml);
game.AddMarket(totals);

// Create same-game parlay (domain allows it, but note correlation)
var sameGameParlay = Bet.CreateParlay(
    new Money(50m),
    (game, ml, ml.Outcomes[0]),
    (game, totals, totals.Outcomes[0])
);

Console.WriteLine($"Same-Game Parlay Created: {sameGameParlay.TicketNumber}");
Console.WriteLine($"Note: Real sportsbooks may adjust odds for correlation");
```

### Scenario 8.3: Odds Format Conversions

```csharp
// Working with different odds formats
var decimalOdds = new Odds(1.67m);
var americanEquivalent = decimalOdds.ToAmerican();  // -150

Console.WriteLine($"Decimal {decimalOdds.DecimalValue} = American {americanEquivalent}");

// Create from American
var favorite = Odds.FromAmerican(-200);   // Heavy favorite
var underdog = Odds.FromAmerican(+350);   // Big underdog

Console.WriteLine($"American -200 = Decimal {favorite.DecimalValue:F2}");  // 1.50
Console.WriteLine($"American +350 = Decimal {underdog.DecimalValue:F2}");  // 4.50
```

### Scenario 8.4: Zero Stake Bet (Edge Case)

```csharp
// System allows zero stake (implementation-dependent if this should be allowed)
var zeroBet = Bet.CreateSingle(
    new Money(0m),
    game,
    market,
    outcome
);

Console.WriteLine($"Zero stake bet: {zeroBet.TicketNumber}");
Console.WriteLine($"Potential payout: {zeroBet.PotentialPayout}");  // 0.00 USD
```

### Scenario 8.5: Currency Handling

```csharp
// Different currencies
var usdBet = new Money(100m, "USD");
var eurBet = new Money(100m, "EUR");

Console.WriteLine($"USD Bet: {usdBet}");
Console.WriteLine($"EUR Bet: {eurBet}");

// Cannot mix currencies
try
{
    var invalid = usdBet + eurBet;  // Throws InvalidOperationException
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

---

## Running the Demo

To see all these scenarios in action:

```bash
cd SportsBetting
dotnet run --project SportsBetting.Console
```

This runs:
- 5 demonstration scenarios
- 50 edge case tests
- Full settlement examples
- LineLock demonstrations

---

## Key Takeaways

### ✅ Type Safety
- `Money` prevents negative amounts and currency mismatches
- `Odds` validates values ≥ 1.0 and handles conversions
- `Score` provides computed properties (margin, total, winner)

### ✅ Business Rules Enforced
- Cannot bet on closed markets
- Parlay requires 2+ legs
- LineLock expires after event start time
- Push/Void handling returns stakes

### ✅ Immutability
- Once bet is placed, selections/odds are locked
- Score, Money, Odds are immutable structs
- State changes through explicit methods only

### ✅ Rich Domain Model
- Entities contain behavior, not just data
- `bet.PotentialPayout` calculates automatically
- `game.Complete(score)` manages state transitions
- Settlement logic encapsulated in service

### ✅ No External Dependencies
- Pure C# domain logic
- No database, no API calls
- Perfect for testing and validation

---

## Next Steps

1. **Add Persistence**: Create EF Core entities mapping to this domain model
2. **Add API**: REST endpoints wrapping domain operations
3. **Add Wallet Service**: User balance management
4. **Add P2P Exchange**: BetOffer entity and matching engine
5. **Add In-Play**: Live odds updates and market suspensions

---

## Need Help?

- Check `README.md` for project overview
- Check `SportsBetting.Console/Program.cs` for complete examples
- Check `Documentation.html` for API reference
- Domain code is in `SportsBetting.Domain/`

## Testing

Run all 50 edge case tests:
```bash
dotnet run --project SportsBetting.Console
```

All tests are in the `Program.cs` file under the `Test*EdgeCases()` methods.
