# Sports Betting Domain Model

A pure C# object-oriented domain model for a sports betting exchange platform, designed to support both traditional bookmaker and peer-to-peer betting models.

## Overview

This library implements the core business logic for a sports betting system as outlined in the PDF specification. It includes:

- **Pre-game betting** (in-play betting structure is in place but not fully implemented)
- **Multiple bet types**: Single bets, Parlays (accumulators)
- **Market types**: Moneyline, Spread, Totals, Prop, Futures
- **Full settlement logic** with void/push handling
- **Bookmaker model** (P2P exchange entities ready to be added)

## Project Structure

```
SportsBetting/                      # Master solution directory
├── SportsBetting.sln              # Master solution file
│
├── SportsBetting.Domain/          # Core domain library (no dependencies)
│   ├── Entities/                  # Domain entities
│   │   ├── Sport.cs              # Sport/discipline
│   │   ├── League.cs             # League/tournament
│   │   ├── Team.cs               # Team/participant
│   │   ├── Event.cs              # Sporting event/match
│   │   ├── Market.cs             # Betting market
│   │   ├── Outcome.cs            # Market outcome
│   │   ├── BetSelection.cs       # Selection within a bet
│   │   └── Bet.cs                # Betting ticket/slip
│   ├── ValueObjects/             # Immutable value objects
│   │   ├── Money.cs              # Type-safe money handling
│   │   ├── Odds.cs               # Odds with payout calculations
│   │   └── Score.cs              # Event score
│   ├── Enums/                    # Enumerations
│   ├── Services/                 # Domain services
│   │   └── SettlementService.cs  # Bet settlement logic
│   └── Exceptions/               # Domain exceptions
│
├── SportsBetting.Services/        # Application services (PLACEHOLDER)
│   ├── Interfaces/               # Service contracts
│   └── Implementations/          # Service implementations
│
├── SportsBetting.API/             # REST API (PLACEHOLDER)
│   ├── Controllers/              # API endpoints
│   └── Program.cs                # Startup + DI configuration
│
└── SportsBetting.Console/         # Demo/testing console app
    └── Program.cs                 # Demonstration scenarios
```

**Layer Dependencies:**
- API → Services → Domain
- Console → Domain (for testing)
- Services and API are currently placeholders

## Key Design Principles

### 1. Rich Domain Model
Entities contain business logic, not just data:
```csharp
// Bet calculates its own payout
var payout = bet.PotentialPayout;

// Event manages its own state transitions
event.Complete(finalScore);
event.Cancel();
```

### 2. Value Objects for Type Safety
```csharp
// Money prevents negative amounts, enforces currency
var stake = new Money(100m, "USD");

// Odds handles format conversions and payout calculations
var odds = Odds.FromAmerican(-150);
var payout = odds.CalculatePayout(stake);
```

### 3. Immutability Where Appropriate
- Value objects (Money, Odds, Score) are immutable structs
- Once a Bet is placed, its selections and odds are locked
- State changes happen through explicit methods

### 4. No Persistence Concerns
- Pure domain logic, no database dependencies
- No Entity Framework attributes or data annotations
- This model can be used as a reference for creating EF entities later

## Usage Examples

### Creating a Single Bet

```csharp
// Setup
var nfl = new League("NFL", "NFL", sportId);
var chiefs = new Team("Chiefs", "KC", nfl.Id, "Kansas City");
var ravens = new Team("Ravens", "BAL", nfl.Id, "Baltimore");

var game = new Event(
    "Chiefs vs Ravens",
    homeTeam: chiefs,
    awayTeam: ravens,
    scheduledStartTime: DateTime.UtcNow.AddHours(2),
    leagueId: nfl.Id
);

// Create market with outcomes
var moneyline = new Market(MarketType.Moneyline, "Match Winner");
moneyline.AddOutcome(new Outcome("Chiefs Win", "Chiefs", new Odds(1.90m)));
moneyline.AddOutcome(new Outcome("Ravens Win", "Ravens", new Odds(2.05m)));
game.AddMarket(moneyline);

// Place bet
var stake = new Money(100m, "USD");
var chiefsOutcome = moneyline.Outcomes.First(o => o.Name.Contains("Chiefs"));
var bet = Bet.CreateSingle(stake, game, moneyline, chiefsOutcome);

Console.WriteLine($"Potential Payout: {bet.PotentialPayout}"); // 190.00 USD
```

### Creating a Parlay

```csharp
var parlay = Bet.CreateParlay(
    new Money(50m),
    (game1, market1, outcome1),
    (game2, market2, outcome2),
    (game3, market3, outcome3)
);

Console.WriteLine($"Combined Odds: {parlay.CombinedOdds}"); // 5.79 (1.80 × 1.65 × 1.95)
Console.WriteLine($"Potential: {parlay.PotentialPayout}"); // 289.58 USD
```

### Settlement

```csharp
// Complete the event
game.Complete(new Score(homeScore: 28, awayScore: 24));

// Settle markets
var settlementService = new SettlementService();
settlementService.SettleMoneylineMarket(moneyline, game);

// Settle bets
settlementService.SettleBet(bet, new[] { game });

Console.WriteLine($"Status: {bet.Status}"); // Won
Console.WriteLine($"Payout: {bet.ActualPayout}"); // 190.00 USD
```

### Parlay with Void Leg

```csharp
// If one game is cancelled:
game2.Cancel();
market2.SettleAsVoid();

// Settlement automatically recalculates parlay odds without void legs
settlementService.SettleBet(parlay, new[] { game1, game2 });
// Parlay reduces to single bet on remaining leg
```

### LineLock - Odds Option Contract

```csharp
// Create a LineLock to guarantee odds for 30 minutes
var lineLock = LineLock.Create(
    game,
    market,
    outcome,
    lockFee: new Money(5m),      // Premium paid
    maxStake: new Money(200m),   // Maximum stake covered
    expirationTime: DateTime.UtcNow.AddMinutes(30)
);

Console.WriteLine($"Locked Odds: {lineLock.LockedOdds}"); // 2.20

// Odds move in the market
outcome.UpdateOdds(new Odds(1.85m)); // Dropped to 1.85!

// Exercise lock with $150 (less than max $200)
var bet = lineLock.Exercise(new Money(150m), game, market, outcome);

Console.WriteLine($"Bet Odds: {bet.CombinedOdds}");       // 2.20 (locked!)
Console.WriteLine($"Market Odds: {outcome.CurrentOdds}"); // 1.85 (current)
Console.WriteLine($"Was LineLocked: {bet.WasLineLocked}"); // True
```

## Features Implemented

### ✅ Core Entities
- Sport, League, Team hierarchy
- Event with status management
- Market with multiple outcomes
- Bet with single and parlay support
- **LineLock - Odds option contract (NEW!)**
- Full settlement with void/push handling

### ✅ Market Types
- Moneyline (2-way and 3-way with draw)
- Point Spread with line values
- Totals (Over/Under)
- Structure ready for Props and Futures

### ✅ Bet Types
- Single bets
- Parlays (all legs must win)
- Void handling (recalculates parlay odds)
- Push handling (returns stake)

### ✅ Settlement Logic
- Automatic outcome determination
- Parlay settlement with void leg handling
- Push scenarios (exact line hits)
- Event cancellation support

### ✅ Value Objects
- Money with currency support
- Odds (decimal format with American conversion)
- Score with helper properties

### ✅ LineLock Features
- Lock in odds by paying a premium (like an option contract)
- Expiration time management
- Exercise with custom stake (up to max stake)
- Automatic expiration if not exercised
- Fee refunds if event cancelled
- Bet tracking (LineLock → Bet relationship)

## Future Additions (Not Yet Implemented)

### P2P Exchange Features
- `BetOffer` entity (order book)
- Matching engine
- Back/Lay positions
- Liquidity management

### Advanced Bet Types
- System bets / Round Robins
- Same-game parlays with correlation handling
- Bet builders

### In-Play Betting
- Live odds updates
- Market suspensions during play
- Event state tracking (score, time, period)

### Financial Features
- Cash-out functionality
- Free bet support
- Bonus tracking

### User Management
- User accounts
- Wallet/balance management
- Bet history
- Transaction ledger

## Running the Demo

```bash
cd SportsBetting
dotnet build
dotnet run --project SportsBetting.Console
```

The demo runs 5 scenarios:
1. Single moneyline bet (win scenario)
2. 3-leg parlay (all win)
3. 2-leg parlay with one void leg
4. Spread and totals bets
5. **LineLock** - Locking odds before they move (with exercise & expiration)

## Technology

- **.NET 9.0**
- **C# 13**
- **Pure OOP** - no dependencies, no database
- **Value objects as structs** for performance
- **Immutability** where appropriate
- **Rich domain model** with behavior

## Notes

This is a **domain model only** - no database, API, or UI concerns. When implementing:

- Use this as a reference for Entity Framework entities
- Add persistence layer separately
- User authentication/wallet handled by external services (paper trading for now)
- API layer would wrap these domain objects

The model is designed to be **database-agnostic** and can be mapped to any persistence strategy (EF Core, Dapper, document DB, etc.).
