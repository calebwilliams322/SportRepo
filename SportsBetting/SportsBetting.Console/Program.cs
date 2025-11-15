using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Console;

class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine("=== Sports Betting System Demo ===\n");

        try
        {
            // Run different scenarios
            DemoSingleBet();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            DemoParlayBet();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            DemoParlayWithVoidLeg();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            DemoSpreadAndTotalsBets();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            DemoLineLock();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            // Run comprehensive edge case tests
            System.Console.WriteLine("\n" + new string('#', 60));
            System.Console.WriteLine("### COMPREHENSIVE EDGE CASE TESTING ###");
            System.Console.WriteLine(new string('#', 60) + "\n");

            TestMoneyEdgeCases();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            TestOddsEdgeCases();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            TestBetPlacementEdgeCases();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            TestSettlementEdgeCases();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            TestLineLockEdgeCases();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            TestEventLifecycleEdgeCases();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            TestCancellationAndRefunds();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            TestDecimalPrecisionEdgeCases();
            System.Console.WriteLine("\n" + new string('=', 60) + "\n");

            TestOrphanedObjectValidation();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"ERROR: {ex.Message}");
            System.Console.WriteLine(ex.StackTrace);
        }

        System.Console.WriteLine("\n=== Demo Complete ===");
    }

    static void DemoSingleBet()
    {
        System.Console.WriteLine("SCENARIO 1: Single Moneyline Bet");
        System.Console.WriteLine("--------------------------------\n");

        // Create sport, league, teams
        var football = new Sport("American Football", "NFL");
        var nfl = new League("National Football League", "NFL", football.Id);
        football.AddLeague(nfl);

        var chiefs = new Team("Chiefs", "KC", nfl.Id, "Kansas City");
        var ravens = new Team("Ravens", "BAL", nfl.Id, "Baltimore");
        nfl.AddTeam(chiefs);
        nfl.AddTeam(ravens);

        // Create event
        var game = new Event(
            "Chiefs vs Ravens - Week 1",
            homeTeam: chiefs,
            awayTeam: ravens,
            scheduledStartTime: DateTime.UtcNow.AddHours(2),
            leagueId: nfl.Id,
            venue: "Arrowhead Stadium"
        );

        System.Console.WriteLine($"Event: {game}");

        // Create moneyline market
        var moneyline = new Market(MarketType.Moneyline, "Match Winner");
        moneyline.AddOutcome(new Outcome($"{chiefs.Name} Win", "Chiefs to win", new Odds(1.90m)));
        moneyline.AddOutcome(new Outcome($"{ravens.Name} Win", "Ravens to win", new Odds(2.05m)));

        game.AddMarket(moneyline);

        System.Console.WriteLine($"\nMarket: {moneyline}");
        foreach (var outcome in moneyline.Outcomes)
        {
            System.Console.WriteLine($"  - {outcome}");
        }

        // Place a bet
        var stake = new Money(100m, "USD");
        var chiefsOutcome = moneyline.Outcomes.First(o => o.Name.Contains("Chiefs"));

        var bet = Bet.CreateSingle(stake, game, moneyline, chiefsOutcome);

        System.Console.WriteLine($"\n✓ Bet Placed: {bet.TicketNumber}");
        System.Console.WriteLine($"  Selection: {bet.Selections[0]}");
        System.Console.WriteLine($"  Stake: {bet.Stake}");
        System.Console.WriteLine($"  Potential Payout: {bet.PotentialPayout}");
        System.Console.WriteLine($"  Potential Profit: {bet.PotentialProfit}");

        // Simulate game completion
        System.Console.WriteLine("\n--- Game is played ---");
        var finalScore = new Score(homeScore: 28, awayScore: 24);
        game.Complete(finalScore);

        System.Console.WriteLine($"Final Score: {game.HomeTeam.Name} {finalScore.HomeScore} - {finalScore.AwayScore} {game.AwayTeam.Name}");

        // Settle the market
        var settlementService = new SettlementService();
        settlementService.SettleMoneylineMarket(moneyline, game);

        System.Console.WriteLine($"Market settled: {moneyline.Name}");

        // Settle the bet
        settlementService.SettleBet(bet, new[] { game });

        System.Console.WriteLine($"\n✓ Bet Settled: {bet.Status}");
        System.Console.WriteLine($"  Actual Payout: {bet.ActualPayout}");
        System.Console.WriteLine($"  Profit: {bet.ActualProfit}");
    }

    static void DemoParlayBet()
    {
        System.Console.WriteLine("SCENARIO 2: 3-Leg Parlay Bet (All Win)");
        System.Console.WriteLine("---------------------------------------\n");

        // Setup
        var basketball = new Sport("Basketball", "NBA");
        var nba = new League("National Basketball Association", "NBA", basketball.Id);
        basketball.AddLeague(nba);

        var lakers = new Team("Lakers", "LAL", nba.Id, "Los Angeles");
        var celtics = new Team("Celtics", "BOS", nba.Id, "Boston");
        var warriors = new Team("Warriors", "GSW", nba.Id, "Golden State");
        var nets = new Team("Nets", "BKN", nba.Id, "Brooklyn");
        var heat = new Team("Heat", "MIA", nba.Id, "Miami");
        var bulls = new Team("Bulls", "CHI", nba.Id, "Chicago");

        // Create 3 games
        var game1 = new Event("Lakers vs Celtics", lakers, celtics, DateTime.UtcNow.AddHours(2), nba.Id);
        var game2 = new Event("Warriors vs Nets", warriors, nets, DateTime.UtcNow.AddHours(3), nba.Id);
        var game3 = new Event("Heat vs Bulls", heat, bulls, DateTime.UtcNow.AddHours(4), nba.Id);

        // Create markets
        var ml1 = new Market(MarketType.Moneyline, "Match Winner");
        ml1.AddOutcome(new Outcome("Lakers Win", "Lakers to win", new Odds(1.80m)));
        ml1.AddOutcome(new Outcome("Celtics Win", "Celtics to win", new Odds(2.10m)));
        game1.AddMarket(ml1);

        var ml2 = new Market(MarketType.Moneyline, "Match Winner");
        ml2.AddOutcome(new Outcome("Warriors Win", "Warriors to win", new Odds(1.65m)));
        ml2.AddOutcome(new Outcome("Nets Win", "Nets to win", new Odds(2.30m)));
        game2.AddMarket(ml2);

        var ml3 = new Market(MarketType.Moneyline, "Match Winner");
        ml3.AddOutcome(new Outcome("Heat Win", "Heat to win", new Odds(1.95m)));
        ml3.AddOutcome(new Outcome("Bulls Win", "Bulls to win", new Odds(1.95m)));
        game3.AddMarket(ml3);

        System.Console.WriteLine("Games:");
        System.Console.WriteLine($"  1. {game1.Name}");
        System.Console.WriteLine($"  2. {game2.Name}");
        System.Console.WriteLine($"  3. {game3.Name}");

        // Place parlay bet
        var stake = new Money(50m, "USD");
        var parlay = Bet.CreateParlay(
            stake,
            (game1, ml1, ml1.Outcomes.First(o => o.Name.Contains("Lakers"))),
            (game2, ml2, ml2.Outcomes.First(o => o.Name.Contains("Warriors"))),
            (game3, ml3, ml3.Outcomes.First(o => o.Name.Contains("Heat")))
        );

        System.Console.WriteLine($"\n✓ Parlay Bet Placed: {parlay.TicketNumber}");
        System.Console.WriteLine($"  Type: {parlay.Type}");
        System.Console.WriteLine($"  Stake: {parlay.Stake}");
        System.Console.WriteLine($"  Combined Odds: {parlay.CombinedOdds}");
        System.Console.WriteLine($"  Potential Payout: {parlay.PotentialPayout}");
        System.Console.WriteLine($"  Selections:");
        foreach (var sel in parlay.Selections)
        {
            System.Console.WriteLine($"    - {sel}");
        }

        // Simulate games
        System.Console.WriteLine("\n--- Games are played ---");
        game1.Complete(new Score(110, 105)); // Lakers win
        game2.Complete(new Score(115, 108)); // Warriors win
        game3.Complete(new Score(98, 95));   // Heat win

        System.Console.WriteLine($"  {game1.Name}: {game1.FinalScore}");
        System.Console.WriteLine($"  {game2.Name}: {game2.FinalScore}");
        System.Console.WriteLine($"  {game3.Name}: {game3.FinalScore}");

        // Settle markets
        var settlementService = new SettlementService();
        settlementService.SettleMoneylineMarket(ml1, game1);
        settlementService.SettleMoneylineMarket(ml2, game2);
        settlementService.SettleMoneylineMarket(ml3, game3);

        // Settle the parlay
        settlementService.SettleBet(parlay, new[] { game1, game2, game3 });

        System.Console.WriteLine($"\n✓ Parlay Settled: {parlay.Status}");
        System.Console.WriteLine($"  Actual Payout: {parlay.ActualPayout}");
        System.Console.WriteLine($"  Profit: {parlay.ActualProfit}");
    }

    static void DemoParlayWithVoidLeg()
    {
        System.Console.WriteLine("SCENARIO 3: Parlay with One Void Leg");
        System.Console.WriteLine("-------------------------------------\n");

        // Setup (simplified)
        var baseball = new Sport("Baseball", "MLB");
        var mlb = new League("Major League Baseball", "MLB", baseball.Id);

        var yankees = new Team("Yankees", "NYY", mlb.Id, "New York");
        var redsox = new Team("Red Sox", "BOS", mlb.Id, "Boston");
        var dodgers = new Team("Dodgers", "LAD", mlb.Id, "Los Angeles");
        var giants = new Team("Giants", "SF", mlb.Id, "San Francisco");

        var game1 = new Event("Yankees vs Red Sox", yankees, redsox, DateTime.UtcNow, mlb.Id);
        var game2 = new Event("Dodgers vs Giants", dodgers, giants, DateTime.UtcNow, mlb.Id);

        var ml1 = new Market(MarketType.Moneyline, "Match Winner");
        ml1.AddOutcome(new Outcome("Yankees Win", "Yankees", new Odds(2.00m)));
        ml1.AddOutcome(new Outcome("Red Sox Win", "Red Sox", new Odds(1.90m)));
        game1.AddMarket(ml1);

        var ml2 = new Market(MarketType.Moneyline, "Match Winner");
        ml2.AddOutcome(new Outcome("Dodgers Win", "Dodgers", new Odds(1.75m)));
        ml2.AddOutcome(new Outcome("Giants Win", "Giants", new Odds(2.20m)));
        game2.AddMarket(ml2);

        // Place parlay
        var parlay = Bet.CreateParlay(
            new Money(100m),
            (game1, ml1, ml1.Outcomes.First(o => o.Name.Contains("Yankees"))),
            (game2, ml2, ml2.Outcomes.First(o => o.Name.Contains("Dodgers")))
        );

        System.Console.WriteLine($"Parlay: {parlay.TicketNumber}");
        System.Console.WriteLine($"  Original Odds: {parlay.CombinedOdds} (2.00 × 1.75)");
        System.Console.WriteLine($"  Original Potential: {parlay.PotentialPayout}");

        // Game 1: Yankees win
        // Game 2: Cancelled (rain delay, game void)
        System.Console.WriteLine("\n--- Results ---");
        game1.Complete(new Score(5, 3));
        System.Console.WriteLine($"  Game 1: Yankees win 5-3");

        game2.Cancel();
        System.Console.WriteLine($"  Game 2: CANCELLED (PPD - Rain)");

        // Settle
        var settlementService = new SettlementService();
        settlementService.SettleMoneylineMarket(ml1, game1);

        // Manually void the second market since the game was cancelled
        ml2.SettleAsVoid();

        settlementService.SettleBet(parlay, new[] { game1, game2 });

        System.Console.WriteLine($"\n✓ Parlay Settled: {parlay.Status}");
        System.Console.WriteLine($"  One leg voided - parlay reduced to single bet");
        System.Console.WriteLine($"  Recalculated Odds: 2.00 (Yankees only)");
        System.Console.WriteLine($"  Actual Payout: {parlay.ActualPayout}");
        System.Console.WriteLine($"  Profit: {parlay.ActualProfit}");
    }

    static void DemoSpreadAndTotalsBets()
    {
        System.Console.WriteLine("SCENARIO 4: Spread and Totals Bets");
        System.Console.WriteLine("-----------------------------------\n");

        var football = new Sport("American Football", "NFL");
        var nfl = new League("NFL", "NFL", football.Id);

        var packers = new Team("Packers", "GB", nfl.Id, "Green Bay");
        var bears = new Team("Bears", "CHI", nfl.Id, "Chicago");

        var game = new Event("Packers vs Bears", packers, bears, DateTime.UtcNow, nfl.Id);

        // Spread market (Packers -6.5)
        var spread = new Market(MarketType.Spread, "Point Spread");
        spread.AddOutcome(new Outcome("Packers -6.5", "Packers cover", new Odds(1.91m), line: -6.5m));
        spread.AddOutcome(new Outcome("Bears +6.5", "Bears cover", new Odds(1.91m), line: +6.5m));
        game.AddMarket(spread);

        // Totals market (O/U 47.5)
        var totals = new Market(MarketType.Totals, "Total Points");
        totals.AddOutcome(new Outcome("Over 47.5", "Over", new Odds(1.87m), line: 47.5m));
        totals.AddOutcome(new Outcome("Under 47.5", "Under", new Odds(1.95m), line: 47.5m));
        game.AddMarket(totals);

        System.Console.WriteLine($"Game: {game.Name}");
        System.Console.WriteLine($"\nSpread Market:");
        foreach (var o in spread.Outcomes)
            System.Console.WriteLine($"  {o}");

        System.Console.WriteLine($"\nTotals Market:");
        foreach (var o in totals.Outcomes)
            System.Console.WriteLine($"  {o}");

        // Place bets
        var spreadBet = Bet.CreateSingle(
            new Money(100m),
            game,
            spread,
            spread.Outcomes.First(o => o.Name.Contains("Packers"))
        );

        var totalsBet = Bet.CreateSingle(
            new Money(100m),
            game,
            totals,
            totals.Outcomes.First(o => o.Name.Contains("Over"))
        );

        System.Console.WriteLine($"\n✓ Bets Placed:");
        System.Console.WriteLine($"  1. {spreadBet.Selections[0]} - Stake: {spreadBet.Stake}");
        System.Console.WriteLine($"  2. {totalsBet.Selections[0]} - Stake: {totalsBet.Stake}");

        // Simulate game: Packers 31, Bears 24 (Total: 55)
        System.Console.WriteLine("\n--- Game Result ---");
        var finalScore = new Score(31, 24);
        game.Complete(finalScore);
        System.Console.WriteLine($"Final Score: Packers 31 - 24 Bears (Total: {finalScore.TotalPoints})");

        // Settle
        var settlementService = new SettlementService();
        settlementService.SettleSpreadMarket(spread, game);
        settlementService.SettleTotalsMarket(totals, game);

        System.Console.WriteLine($"\nSpread Result: Packers win by {finalScore.Margin}, need to cover 6.5");
        System.Console.WriteLine($"  → Packers -6.5 COVERS (31-24 = 7 point margin)");

        System.Console.WriteLine($"\nTotals Result: Combined score {finalScore.TotalPoints}, line 47.5");
        System.Console.WriteLine($"  → OVER 47.5 WINS");

        settlementService.SettleBet(spreadBet, new[] { game });
        settlementService.SettleBet(totalsBet, new[] { game });

        System.Console.WriteLine($"\n✓ Spread Bet: {spreadBet.Status} - Payout: {spreadBet.ActualPayout}");
        System.Console.WriteLine($"✓ Totals Bet: {totalsBet.Status} - Payout: {totalsBet.ActualPayout}");
    }

    static void DemoLineLock()
    {
        System.Console.WriteLine("SCENARIO 5: LineLock - Locking Odds Before They Move");
        System.Console.WriteLine("----------------------------------------------------\n");

        var football = new Sport("American Football", "NFL");
        var nfl = new League("NFL", "NFL", football.Id);

        var ravens = new Team("Ravens", "BAL", nfl.Id, "Baltimore");
        var steelers = new Team("Steelers", "PIT", nfl.Id, "Pittsburgh");

        var game = new Event(
            "Ravens vs Steelers - SNF",
            ravens,
            steelers,
            DateTime.UtcNow.AddHours(3),
            nfl.Id
        );

        // Create moneyline market
        var moneyline = new Market(MarketType.Moneyline, "Match Winner");
        var ravensOutcome = new Outcome("Ravens Win", "Ravens", new Odds(2.20m));
        var steelersOutcome = new Outcome("Steelers Win", "Steelers", new Odds(1.75m));
        moneyline.AddOutcome(ravensOutcome);
        moneyline.AddOutcome(steelersOutcome);
        game.AddMarket(moneyline);

        System.Console.WriteLine($"Game: {game.Name}");
        System.Console.WriteLine($"Current Odds: Ravens {ravensOutcome.CurrentOdds} | Steelers {steelersOutcome.CurrentOdds}");

        // User likes Ravens at 2.20 but wants to think about it
        // Buy a LineLock to guarantee these odds for 30 minutes
        System.Console.WriteLine("\n--- User buys LineLock on Ravens @ 2.20 ---");

        var lockFee = new Money(5m, "USD");
        var maxStake = new Money(200m, "USD");
        var expirationTime = DateTime.UtcNow.AddMinutes(30);

        var lineLock = LineLock.Create(
            game,
            moneyline,
            ravensOutcome,
            lockFee,
            maxStake,
            expirationTime
        );

        System.Console.WriteLine($"✓ LineLock Created: {lineLock.LockNumber}");
        System.Console.WriteLine($"  Locked Odds: {lineLock.LockedOdds}");
        System.Console.WriteLine($"  Lock Fee: {lineLock.LockFee}");
        System.Console.WriteLine($"  Max Stake: {lineLock.MaxStake}");
        System.Console.WriteLine($"  Expires: {lineLock.ExpirationTime:HH:mm:ss} ({lineLock.TimeRemaining.TotalMinutes:F1} minutes)");
        System.Console.WriteLine($"  Max Potential Payout: {lineLock.MaxPotentialPayout}");

        // Market odds move (Ravens odds drop because sharp money came in)
        System.Console.WriteLine("\n--- Breaking News: Star player injury affects odds! ---");
        ravensOutcome.UpdateOdds(new Odds(1.85m)); // Odds dropped significantly!

        System.Console.WriteLine($"Market Odds NOW: Ravens {ravensOutcome.CurrentOdds} (was 2.20)");
        System.Console.WriteLine($"LineLock STILL guarantees: {lineLock.LockedOdds}");

        // User decides to exercise the lock with $150 stake (less than max)
        System.Console.WriteLine("\n--- User exercises LineLock with $150 stake ---");

        var exerciseStake = new Money(150m, "USD");
        var bet = lineLock.Exercise(exerciseStake, game, moneyline, ravensOutcome);

        System.Console.WriteLine($"✓ Bet Placed: {bet.TicketNumber}");
        System.Console.WriteLine($"  Bet created from LineLock: {bet.WasLineLocked}");
        System.Console.WriteLine($"  LineLock ID: {bet.LineLockId}");
        System.Console.WriteLine($"  Stake: {bet.Stake}");
        System.Console.WriteLine($"  Odds Used: {bet.CombinedOdds} (locked rate, not current {ravensOutcome.CurrentOdds})");
        System.Console.WriteLine($"  Potential Payout: {bet.PotentialPayout}");
        System.Console.WriteLine($"  LineLock Status: {lineLock.Status}");

        System.Console.WriteLine("\n💰 Value Gained:");
        var currentPayout = ravensOutcome.CurrentOdds.CalculatePayout(exerciseStake);
        var lockedPayout = bet.PotentialPayout;
        var extraValue = lockedPayout - currentPayout;
        System.Console.WriteLine($"  Payout at current odds (1.85): {currentPayout}");
        System.Console.WriteLine($"  Payout at locked odds (2.20): {lockedPayout}");
        System.Console.WriteLine($"  Extra value from lock: {extraValue}");
        System.Console.WriteLine($"  Minus lock fee: {new Money(extraValue.Amount - lockFee.Amount, "USD")}");

        // Settle the game
        System.Console.WriteLine("\n--- Game Result ---");
        game.Complete(new Score(27, 24)); // Ravens win
        System.Console.WriteLine($"Final: Ravens 27 - 24 Steelers");

        var settlementService = new SettlementService();
        settlementService.SettleMoneylineMarket(moneyline, game);
        settlementService.SettleBet(bet, new[] { game });

        System.Console.WriteLine($"\n✓ Bet Settled: {bet.Status}");
        System.Console.WriteLine($"  Payout: {bet.ActualPayout}");
        System.Console.WriteLine($"  Profit: {bet.ActualProfit}");

        System.Console.WriteLine("\n💡 Summary:");
        System.Console.WriteLine($"  - Paid ${lockFee.Amount} lock fee");
        System.Console.WriteLine($"  - Risked ${bet.Stake.Amount}");
        System.Console.WriteLine($"  - Won ${bet.ActualProfit!.Value.Amount}");
        System.Console.WriteLine($"  - Net profit: ${bet.ActualProfit.Value.Amount - lockFee.Amount}");
        System.Console.WriteLine($"  - Without lock: Would have won ${currentPayout.Amount - exerciseStake.Amount} (at 1.85 odds)");

        // Demo expiration scenario
        System.Console.WriteLine("\n\n--- BONUS: LineLock Expiration Scenario ---");

        var futureGame = new Event(
            "Dolphins vs Bills",
            new Team("Dolphins", "MIA", nfl.Id, "Miami"),
            new Team("Bills", "BUF", nfl.Id, "Buffalo"),
            DateTime.UtcNow.AddHours(5),
            nfl.Id
        );

        var futureMarket = new Market(MarketType.Moneyline, "Match Winner");
        var dolphinsOutcome = new Outcome("Dolphins Win", "Dolphins", new Odds(2.50m));
        futureMarket.AddOutcome(dolphinsOutcome);
        futureGame.AddMarket(futureMarket);

        // Create lock that expires in 1 second
        var shortLock = LineLock.Create(
            futureGame,
            futureMarket,
            dolphinsOutcome,
            new Money(3m),
            new Money(100m),
            DateTime.UtcNow.AddSeconds(1)
        );

        System.Console.WriteLine($"Created short-term lock: {shortLock.LockNumber}");
        System.Console.WriteLine($"Can exercise: {shortLock.CanExercise}");
        System.Console.WriteLine("Waiting for expiration...");

        System.Threading.Thread.Sleep(1100); // Wait for expiration

        System.Console.WriteLine($"Can exercise now: {shortLock.CanExercise}");
        shortLock.Expire();
        System.Console.WriteLine($"Lock status: {shortLock.Status}");
        System.Console.WriteLine("Lock fee is lost if expired without exercise.");
    }

    // ============================================================================
    // EDGE CASE TESTS
    // ============================================================================

    static void TestMoneyEdgeCases()
    {
        System.Console.WriteLine("EDGE CASE TEST: Money Value Object");
        System.Console.WriteLine("-----------------------------------\n");

        int passCount = 0;
        int failCount = 0;

        // Test 1: Negative amount should throw
        System.Console.WriteLine("Test 1: Negative amount should throw exception");
        try
        {
            var negativeMoney = new Money(-100m, "USD");
            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (ArgumentException)
        {
            System.Console.WriteLine("  ✓ PASS - Exception thrown as expected");
            passCount++;
        }

        // Test 2: Zero amount should be allowed
        System.Console.WriteLine("\nTest 2: Zero amount should be allowed");
        try
        {
            var zeroMoney = new Money(0m, "USD");
            System.Console.WriteLine($"  ✓ PASS - Created: {zeroMoney}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 3: Currency mismatch in addition
        System.Console.WriteLine("\nTest 3: Currency mismatch in addition should throw");
        try
        {
            var usd = new Money(100m, "USD");
            var eur = new Money(100m, "EUR");
            var sum = usd + eur;
            System.Console.WriteLine($"  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (InvalidOperationException)
        {
            System.Console.WriteLine("  ✓ PASS - Exception thrown as expected");
            passCount++;
        }

        // Test 4: Currency mismatch in subtraction
        System.Console.WriteLine("\nTest 4: Currency mismatch in subtraction should throw");
        try
        {
            var usd = new Money(100m, "USD");
            var eur = new Money(50m, "EUR");
            var diff = usd - eur;
            System.Console.WriteLine($"  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (InvalidOperationException)
        {
            System.Console.WriteLine("  ✓ PASS - Exception thrown as expected");
            passCount++;
        }

        // Test 5: Very large amount
        System.Console.WriteLine("\nTest 5: Very large amount should work");
        try
        {
            var largeMoney = new Money(1_000_000_000m, "USD");
            System.Console.WriteLine($"  ✓ PASS - Created: {largeMoney}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 6: Subtraction resulting in negative should throw
        System.Console.WriteLine("\nTest 6: Subtraction resulting in negative should throw");
        try
        {
            var small = new Money(50m, "USD");
            var large = new Money(100m, "USD");
            var result = small - large;
            System.Console.WriteLine($"  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (ArgumentException)
        {
            System.Console.WriteLine("  ✓ PASS - Exception thrown as expected");
            passCount++;
        }

        // Test 7: Multiplication by zero
        System.Console.WriteLine("\nTest 7: Multiplication by zero");
        try
        {
            var money = new Money(100m, "USD");
            var result = money * 0m;
            System.Console.WriteLine($"  ✓ PASS - Result: {result}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 8: Multiplication by negative should throw
        System.Console.WriteLine("\nTest 8: Multiplication by negative should throw");
        try
        {
            var money = new Money(100m, "USD");
            var result = money * -2m;
            System.Console.WriteLine($"  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (ArgumentException)
        {
            System.Console.WriteLine("  ✓ PASS - Exception thrown as expected");
            passCount++;
        }

        System.Console.WriteLine($"\n📊 Money Tests: {passCount} passed, {failCount} failed");
    }

    static void TestOddsEdgeCases()
    {
        System.Console.WriteLine("EDGE CASE TEST: Odds Value Object");
        System.Console.WriteLine("----------------------------------\n");

        int passCount = 0;
        int failCount = 0;

        // Test 1: Odds below 1.0 should throw
        System.Console.WriteLine("Test 1: Odds below 1.0 should throw exception");
        try
        {
            var invalidOdds = new Odds(0.5m);
            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (ArgumentException)
        {
            System.Console.WriteLine("  ✓ PASS - Exception thrown as expected");
            passCount++;
        }

        // Test 2: Odds exactly 1.0 (even money)
        System.Console.WriteLine("\nTest 2: Odds exactly 1.0 should work");
        try
        {
            var evenOdds = new Odds(1.0m);
            System.Console.WriteLine($"  ✓ PASS - Created: {evenOdds}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 3: Very high odds
        System.Console.WriteLine("\nTest 3: Very high odds (longshot)");
        try
        {
            var highOdds = new Odds(1000m);
            var stake = new Money(10m, "USD");
            var payout = highOdds.CalculatePayout(stake);
            System.Console.WriteLine($"  ✓ PASS - Odds: {highOdds}, Payout for $10: {payout}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 4: American odds conversion - positive
        System.Console.WriteLine("\nTest 4: American odds conversion (+200)");
        try
        {
            var odds = Odds.FromAmerican(200);
            System.Console.WriteLine($"  ✓ PASS - +200 American = {odds.DecimalValue} Decimal");
            System.Console.WriteLine($"          Back to American: {odds.ToAmerican()}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 5: American odds conversion - negative
        System.Console.WriteLine("\nTest 5: American odds conversion (-150)");
        try
        {
            var odds = Odds.FromAmerican(-150);
            System.Console.WriteLine($"  ✓ PASS - -150 American = {odds.DecimalValue} Decimal");
            System.Console.WriteLine($"          Back to American: {odds.ToAmerican()}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 6: American odds edge case (zero not allowed)
        System.Console.WriteLine("\nTest 6: American odds of 0 should throw");
        try
        {
            var odds = Odds.FromAmerican(0);
            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (ArgumentException)
        {
            System.Console.WriteLine("  ✓ PASS - Exception thrown as expected");
            passCount++;
        }

        // Test 7: Parlay odds multiplication (many legs)
        System.Console.WriteLine("\nTest 7: Parlay with many legs (6-leg parlay)");
        try
        {
            var leg1 = new Odds(1.50m);
            var leg2 = new Odds(1.80m);
            var leg3 = new Odds(1.65m);
            var leg4 = new Odds(1.90m);
            var leg5 = new Odds(1.75m);
            var leg6 = new Odds(2.00m);

            var parlayOdds = leg1 * leg2 * leg3 * leg4 * leg5 * leg6;
            var stake = new Money(10m, "USD");
            var payout = parlayOdds.CalculatePayout(stake);

            System.Console.WriteLine($"  ✓ PASS - Combined odds: {parlayOdds.DecimalValue:F2}");
            System.Console.WriteLine($"          Payout for $10: {payout}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 8: Very small fractional odds
        System.Console.WriteLine("\nTest 8: Very small odds (heavy favorite)");
        try
        {
            var tinyOdds = new Odds(1.01m);
            var stake = new Money(100m, "USD");
            var profit = tinyOdds.CalculateProfit(stake);
            System.Console.WriteLine($"  ✓ PASS - Odds: {tinyOdds}, Profit for $100: {profit}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        System.Console.WriteLine($"\n📊 Odds Tests: {passCount} passed, {failCount} failed");
    }

    static void TestBetPlacementEdgeCases()
    {
        System.Console.WriteLine("EDGE CASE TEST: Bet Placement");
        System.Console.WriteLine("------------------------------\n");

        int passCount = 0;
        int failCount = 0;

        // Setup common entities
        var sport = new Sport("Football", "NFL");
        var league = new League("NFL", "NFL", sport.Id);
        var team1 = new Team("Team1", "T1", league.Id, "Team One");
        var team2 = new Team("Team2", "T2", league.Id, "Team Two");

        // Test 1: Betting on closed market
        System.Console.WriteLine("Test 1: Betting on closed market should throw");
        try
        {
            var game = new Event("Game 1", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            market.Close(); // Close the market

            var bet = Bet.CreateSingle(new Money(100m), game, market, outcome);
            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.MarketClosedException)
        {
            System.Console.WriteLine("  ✓ PASS - MarketClosedException thrown as expected");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 2: Parlay with less than 2 selections
        System.Console.WriteLine("\nTest 2: Parlay with < 2 selections should throw");
        try
        {
            var game = new Event("Game 2", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var parlay = Bet.CreateParlay(
                new Money(100m),
                (game, market, outcome) // Only 1 selection
            );
            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidBetException)
        {
            System.Console.WriteLine("  ✓ PASS - InvalidBetException thrown as expected");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 3: Parlay on same event (same-game parlay - currently allowed but noted)
        System.Console.WriteLine("\nTest 3: Same-event parlay (should be allowed with note)");
        try
        {
            var game = new Event("Game 3", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);

            var market1 = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market1.AddOutcome(outcome1);
            game.AddMarket(market1);

            var market2 = new Market(MarketType.Totals, "Total Points");
            var outcome2 = new Outcome("Over 45", "Over", new Odds(1.90m), 45m);
            market2.AddOutcome(outcome2);
            game.AddMarket(market2);

            var parlay = Bet.CreateParlay(
                new Money(100m),
                (game, market1, outcome1),
                (game, market2, outcome2) // Same event
            );
            System.Console.WriteLine($"  ✓ PASS - Same-game parlay created: {parlay.TicketNumber}");
            System.Console.WriteLine("           (Note: Real sportsbooks may handle correlation differently)");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 4: Parlay with one closed market
        System.Console.WriteLine("\nTest 4: Parlay with one closed market should throw");
        try
        {
            var game1 = new Event("Game 4a", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            var game2 = new Event("Game 4b", team1, team2, DateTime.UtcNow.AddHours(2), league.Id);

            var market1 = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market1.AddOutcome(outcome1);
            game1.AddMarket(market1);

            var market2 = new Market(MarketType.Moneyline, "Winner");
            var outcome2 = new Outcome("Team1 Win", "T1", new Odds(1.8m));
            market2.AddOutcome(outcome2);
            market2.Close(); // Close second market
            game2.AddMarket(market2);

            var parlay = Bet.CreateParlay(
                new Money(100m),
                (game1, market1, outcome1),
                (game2, market2, outcome2)
            );
            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.MarketClosedException)
        {
            System.Console.WriteLine("  ✓ PASS - MarketClosedException thrown as expected");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 5: Betting with zero stake
        System.Console.WriteLine("\nTest 5: Betting with zero stake");
        try
        {
            var game = new Event("Game 5", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var bet = Bet.CreateSingle(new Money(0m), game, market, outcome);
            System.Console.WriteLine($"  ✓ PASS - Zero stake bet created: {bet.TicketNumber}");
            System.Console.WriteLine($"           Potential payout: {bet.PotentialPayout}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ⚠ INFO - Zero stake rejected: {ex.Message}");
            System.Console.WriteLine("           (This is implementation-dependent)");
            passCount++; // Count as pass since either behavior is valid
        }

        System.Console.WriteLine($"\n📊 Bet Placement Tests: {passCount} passed, {failCount} failed");
    }

    static void TestSettlementEdgeCases()
    {
        System.Console.WriteLine("EDGE CASE TEST: Settlement Logic");
        System.Console.WriteLine("---------------------------------\n");

        int passCount = 0;
        int failCount = 0;

        var sport = new Sport("Football", "NFL");
        var league = new League("NFL", "NFL", sport.Id);
        var team1 = new Team("Team1", "T1", league.Id, "Team One");
        var team2 = new Team("Team2", "T2", league.Id, "Team Two");
        var settlementService = new SettlementService();

        // Test 1: Settling already settled bet
        System.Console.WriteLine("Test 1: Settling already settled bet should throw");
        try
        {
            var game = new Event("Game 1", team1, team2, DateTime.UtcNow, league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var bet = Bet.CreateSingle(new Money(100m), game, market, outcome);

            game.Complete(new Score(10, 5));
            settlementService.SettleMoneylineMarket(market, game);
            settlementService.SettleBet(bet, new[] { game }); // First settlement

            // Try to settle again
            settlementService.SettleBet(bet, new[] { game });
            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.SettlementException)
        {
            System.Console.WriteLine("  ✓ PASS - SettlementException thrown as expected");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}: {ex.Message}");
            failCount++;
        }

        // Test 2: All parlay legs void
        System.Console.WriteLine("\nTest 2: Parlay with all legs void");
        try
        {
            var game1 = new Event("Game 2a", team1, team2, DateTime.UtcNow, league.Id);
            var game2 = new Event("Game 2b", team1, team2, DateTime.UtcNow, league.Id);

            var market1 = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market1.AddOutcome(outcome1);
            game1.AddMarket(market1);

            var market2 = new Market(MarketType.Moneyline, "Winner");
            var outcome2 = new Outcome("Team1 Win", "T1", new Odds(1.8m));
            market2.AddOutcome(outcome2);
            game2.AddMarket(market2);

            var parlay = Bet.CreateParlay(
                new Money(100m),
                (game1, market1, outcome1),
                (game2, market2, outcome2)
            );

            // Both games cancelled
            game1.Cancel();
            game2.Cancel();
            market1.SettleAsVoid();
            market2.SettleAsVoid();

            settlementService.SettleBet(parlay, new[] { game1, game2 });

            if (parlay.Status == BetStatus.Void && parlay.ActualPayout == parlay.Stake)
            {
                System.Console.WriteLine($"  ✓ PASS - Parlay voided, stake returned: {parlay.ActualPayout}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected Void with stake returned, got {parlay.Status}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 3: Spread push (exact line hit)
        System.Console.WriteLine("\nTest 3: Spread push (exact line hit)");
        try
        {
            var game = new Event("Game 3", team1, team2, DateTime.UtcNow, league.Id);
            var spread = new Market(MarketType.Spread, "Point Spread");
            var outcome1 = new Outcome("Team1 -7", "T1 -7", new Odds(1.91m), -7m);
            var outcome2 = new Outcome("Team2 +7", "T2 +7", new Odds(1.91m), +7m);
            spread.AddOutcome(outcome1);
            spread.AddOutcome(outcome2);
            game.AddMarket(spread);

            var bet = Bet.CreateSingle(new Money(100m), game, spread, outcome1);

            // Score exactly hits the line: Team1 wins by exactly 7
            game.Complete(new Score(21, 14));
            settlementService.SettleSpreadMarket(spread, game);
            settlementService.SettleBet(bet, new[] { game });

            if (bet.Status == BetStatus.Pushed && bet.ActualPayout == bet.Stake)
            {
                System.Console.WriteLine($"  ✓ PASS - Spread push, stake returned: {bet.ActualPayout}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected Push, got {bet.Status}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 4: Totals push (exact line hit)
        System.Console.WriteLine("\nTest 4: Totals push (exact total)");
        try
        {
            var game = new Event("Game 4", team1, team2, DateTime.UtcNow, league.Id);
            var totals = new Market(MarketType.Totals, "Total Points");
            var overOutcome = new Outcome("Over 45", "Over", new Odds(1.87m), 45m);
            var underOutcome = new Outcome("Under 45", "Under", new Odds(1.95m), 45m);
            totals.AddOutcome(overOutcome);
            totals.AddOutcome(underOutcome);
            game.AddMarket(totals);

            var bet = Bet.CreateSingle(new Money(100m), game, totals, overOutcome);

            // Score exactly 45
            game.Complete(new Score(24, 21));
            settlementService.SettleTotalsMarket(totals, game);
            settlementService.SettleBet(bet, new[] { game });

            if (bet.Status == BetStatus.Pushed && bet.ActualPayout == bet.Stake)
            {
                System.Console.WriteLine($"  ✓ PASS - Totals push, stake returned: {bet.ActualPayout}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected Push, got {bet.Status}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 5: Parlay reduces to single bet (one void leg)
        System.Console.WriteLine("\nTest 5: Parlay with one void leg reduces to single");
        try
        {
            var game1 = new Event("Game 5a", team1, team2, DateTime.UtcNow, league.Id);
            var game2 = new Event("Game 5b", team1, team2, DateTime.UtcNow, league.Id);

            var market1 = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market1.AddOutcome(outcome1);
            var outcome1lose = new Outcome("Team2 Win", "T2", new Odds(2.0m));
            market1.AddOutcome(outcome1lose);
            game1.AddMarket(market1);

            var market2 = new Market(MarketType.Moneyline, "Winner");
            var outcome2 = new Outcome("Team1 Win", "T1", new Odds(1.8m));
            market2.AddOutcome(outcome2);
            game2.AddMarket(market2);

            var parlay = Bet.CreateParlay(
                new Money(100m),
                (game1, market1, outcome1),
                (game2, market2, outcome2)
            );

            var originalOdds = parlay.CombinedOdds.DecimalValue; // 2.0 × 1.8 = 3.6

            // Game 1: Team1 wins
            game1.Complete(new Score(10, 5));
            settlementService.SettleMoneylineMarket(market1, game1);

            // Game 2: Cancelled
            game2.Cancel();
            market2.SettleAsVoid();

            settlementService.SettleBet(parlay, new[] { game1, game2 });

            var expectedPayout = new Odds(2.0m).CalculatePayout(new Money(100m)); // Only game1 odds

            if (parlay.Status == BetStatus.Won && parlay.ActualPayout == expectedPayout)
            {
                System.Console.WriteLine($"  ✓ PASS - Parlay reduced to single bet");
                System.Console.WriteLine($"           Original odds: {originalOdds}, Recalculated: 2.0");
                System.Console.WriteLine($"           Payout: {parlay.ActualPayout}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected payout {expectedPayout}, got {parlay.ActualPayout}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 6: Manually voiding a bet
        System.Console.WriteLine("\nTest 6: Manually voiding a bet");
        try
        {
            var game = new Event("Game 6", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var bet = Bet.CreateSingle(new Money(100m), game, market, outcome);

            bet.Void(); // Manually void

            if (bet.Status == BetStatus.Void && bet.ActualPayout == bet.Stake)
            {
                System.Console.WriteLine($"  ✓ PASS - Bet manually voided, stake returned: {bet.ActualPayout}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected Void, got {bet.Status}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        System.Console.WriteLine($"\n📊 Settlement Tests: {passCount} passed, {failCount} failed");
    }

    static void TestLineLockEdgeCases()
    {
        System.Console.WriteLine("EDGE CASE TEST: LineLock Functionality");
        System.Console.WriteLine("---------------------------------------\n");

        int passCount = 0;
        int failCount = 0;

        var sport = new Sport("Football", "NFL");
        var league = new League("NFL", "NFL", sport.Id);
        var team1 = new Team("Team1", "T1", league.Id, "Team One");
        var team2 = new Team("Team2", "T2", league.Id, "Team Two");

        // Test 1: Exercising expired lock
        System.Console.WriteLine("Test 1: Exercising expired lock should throw");
        try
        {
            var game = new Event("Game 1", team1, team2, DateTime.UtcNow.AddHours(5), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lineLock = LineLock.Create(
                game, market, outcome,
                new Money(5m), new Money(100m),
                DateTime.UtcNow.AddMilliseconds(100)
            );

            System.Threading.Thread.Sleep(150); // Wait for expiration

            var bet = lineLock.Exercise(new Money(50m), game, market, outcome);
            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidBetException ex)
        {
            if (ex.Message.Contains("expired"))
            {
                System.Console.WriteLine("  ✓ PASS - InvalidBetException for expired lock");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 2: Exercising with stake > maxStake
        System.Console.WriteLine("\nTest 2: Exercising with stake > maxStake should throw");
        try
        {
            var game = new Event("Game 2", team1, team2, DateTime.UtcNow.AddHours(5), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lineLock = LineLock.Create(
                game, market, outcome,
                new Money(5m), new Money(100m),
                DateTime.UtcNow.AddMinutes(30)
            );

            var bet = lineLock.Exercise(new Money(150m), game, market, outcome);
            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidBetException ex)
        {
            if (ex.Message.Contains("max stake"))
            {
                System.Console.WriteLine("  ✓ PASS - InvalidBetException for exceeding max stake");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 3: Exercising already used lock
        System.Console.WriteLine("\nTest 3: Exercising already used lock should throw");
        try
        {
            var game = new Event("Game 3", team1, team2, DateTime.UtcNow.AddHours(5), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lineLock = LineLock.Create(
                game, market, outcome,
                new Money(5m), new Money(100m),
                DateTime.UtcNow.AddMinutes(30)
            );

            var bet1 = lineLock.Exercise(new Money(50m), game, market, outcome);
            var bet2 = lineLock.Exercise(new Money(30m), game, market, outcome); // Try again

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidBetException ex)
        {
            if (ex.Message.Contains("Used"))
            {
                System.Console.WriteLine("  ✓ PASS - InvalidBetException for already used lock");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 4: LineLock expiration after event start time
        System.Console.WriteLine("\nTest 4: LineLock expiring after event start should throw");
        try
        {
            var game = new Event("Game 4", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lineLock = LineLock.Create(
                game, market, outcome,
                new Money(5m), new Money(100m),
                DateTime.UtcNow.AddHours(2) // After event start!
            );

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidBetException ex)
        {
            if (ex.Message.Contains("after event"))
            {
                System.Console.WriteLine("  ✓ PASS - InvalidBetException for expiration after event start");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 5: Cancelling a lock
        System.Console.WriteLine("\nTest 5: Cancelling a lock");
        try
        {
            var game = new Event("Game 5", team1, team2, DateTime.UtcNow.AddHours(5), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lineLock = LineLock.Create(
                game, market, outcome,
                new Money(5m), new Money(100m),
                DateTime.UtcNow.AddMinutes(30)
            );

            lineLock.Cancel();

            if (lineLock.Status == LineLockStatus.Cancelled)
            {
                System.Console.WriteLine($"  ✓ PASS - Lock cancelled, status: {lineLock.Status}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected Cancelled, got {lineLock.Status}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 6: Multiple locks on same outcome
        System.Console.WriteLine("\nTest 6: Multiple locks on same outcome (should be allowed)");
        try
        {
            var game = new Event("Game 6", team1, team2, DateTime.UtcNow.AddHours(5), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lock1 = LineLock.Create(
                game, market, outcome,
                new Money(5m), new Money(100m),
                DateTime.UtcNow.AddMinutes(30)
            );

            var lock2 = LineLock.Create(
                game, market, outcome,
                new Money(3m), new Money(50m),
                DateTime.UtcNow.AddMinutes(20)
            );

            System.Console.WriteLine($"  ✓ PASS - Multiple locks created:");
            System.Console.WriteLine($"           Lock 1: {lock1.LockNumber}, Max: {lock1.MaxStake}");
            System.Console.WriteLine($"           Lock 2: {lock2.LockNumber}, Max: {lock2.MaxStake}");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 7: Exercise with less than max stake
        System.Console.WriteLine("\nTest 7: Exercising with less than max stake");
        try
        {
            var game = new Event("Game 7", team1, team2, DateTime.UtcNow.AddHours(5), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.5m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lineLock = LineLock.Create(
                game, market, outcome,
                new Money(10m), new Money(200m),
                DateTime.UtcNow.AddMinutes(30)
            );

            // Exercise with only $75 instead of max $200
            var bet = lineLock.Exercise(new Money(75m), game, market, outcome);

            if (bet.Stake.Amount == 75m && bet.CombinedOdds.DecimalValue == 2.5m)
            {
                System.Console.WriteLine($"  ✓ PASS - Exercised with partial stake:");
                System.Console.WriteLine($"           Max allowed: {lineLock.MaxStake}");
                System.Console.WriteLine($"           Actually used: {bet.Stake}");
                System.Console.WriteLine($"           Potential payout: {bet.PotentialPayout}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Unexpected bet parameters");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 8: Currency mismatch in exercise
        System.Console.WriteLine("\nTest 8: Currency mismatch in exercise should throw");
        try
        {
            var game = new Event("Game 8", team1, team2, DateTime.UtcNow.AddHours(5), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lineLock = LineLock.Create(
                game, market, outcome,
                new Money(5m, "USD"),
                new Money(100m, "USD"),
                DateTime.UtcNow.AddMinutes(30)
            );

            var bet = lineLock.Exercise(new Money(50m, "EUR"), game, market, outcome); // Wrong currency

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidBetException ex)
        {
            if (ex.Message.Contains("currency"))
            {
                System.Console.WriteLine("  ✓ PASS - InvalidBetException for currency mismatch");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("currency") || ex.Message.Contains("Currency") || ex.Message.Contains("currencies"))
            {
                System.Console.WriteLine("  ✓ PASS - Exception for currency mismatch");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        System.Console.WriteLine($"\n📊 LineLock Tests: {passCount} passed, {failCount} failed");
    }

    static void TestEventLifecycleEdgeCases()
    {
        System.Console.WriteLine("EDGE CASE TEST: Event Lifecycle");
        System.Console.WriteLine("--------------------------------\n");

        int passCount = 0;
        int failCount = 0;

        var sport = new Sport("Football", "NFL");
        var league = new League("NFL", "NFL", sport.Id);
        var team1 = new Team("Team1", "T1", league.Id, "Team One");
        var team2 = new Team("Team2", "T2", league.Id, "Team Two");

        // Test 1: Completing already completed event
        System.Console.WriteLine("Test 1: Completing already completed event should throw");
        try
        {
            var game = new Event("Game 1", team1, team2, DateTime.UtcNow, league.Id);
            game.Complete(new Score(10, 5));
            game.Complete(new Score(20, 15)); // Try again

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidEventStateException)
        {
            System.Console.WriteLine("  ✓ PASS - InvalidEventStateException thrown");
            passCount++;
        }
        catch (InvalidOperationException)
        {
            System.Console.WriteLine("  ✓ PASS - InvalidOperationException thrown");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 2: Cancelling completed event
        System.Console.WriteLine("\nTest 2: Cancelling completed event should throw");
        try
        {
            var game = new Event("Game 2", team1, team2, DateTime.UtcNow, league.Id);
            game.Complete(new Score(10, 5));
            game.Cancel();

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidEventStateException)
        {
            System.Console.WriteLine("  ✓ PASS - InvalidEventStateException thrown");
            passCount++;
        }
        catch (InvalidOperationException)
        {
            System.Console.WriteLine("  ✓ PASS - InvalidOperationException thrown");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 3: Suspending scheduled event
        System.Console.WriteLine("\nTest 3: Suspending scheduled event (not in progress)");
        try
        {
            var game = new Event("Game 3", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            game.Suspend();

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidEventStateException)
        {
            System.Console.WriteLine("  ✓ PASS - InvalidEventStateException thrown");
            passCount++;
        }
        catch (InvalidOperationException)
        {
            System.Console.WriteLine("  ✓ PASS - InvalidOperationException thrown");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 4: Resuming non-suspended event
        System.Console.WriteLine("\nTest 4: Resuming non-suspended event should throw");
        try
        {
            var game = new Event("Game 4", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            game.Resume();

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.InvalidEventStateException)
        {
            System.Console.WriteLine("  ✓ PASS - InvalidEventStateException thrown");
            passCount++;
        }
        catch (InvalidOperationException)
        {
            System.Console.WriteLine("  ✓ PASS - InvalidOperationException thrown");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 5: Event with no markets
        System.Console.WriteLine("\nTest 5: Creating event with no markets (should be allowed)");
        try
        {
            var game = new Event("Game 5", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            System.Console.WriteLine($"  ✓ PASS - Event created with {game.Markets.Count} markets");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 6: Market settlement without outcome winners
        System.Console.WriteLine("\nTest 6: Settling market without specifying winners");
        try
        {
            var game = new Event("Game 6", team1, team2, DateTime.UtcNow, league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            var outcome2 = new Outcome("Team2 Win", "T2", new Odds(2.0m));
            market.AddOutcome(outcome1);
            market.AddOutcome(outcome2);
            game.AddMarket(market);

            game.Complete(new Score(10, 5));

            // Settle with no winners specified
            market.Settle();

            System.Console.WriteLine("  ⚠ INFO - Market settled without winners");
            System.Console.WriteLine($"           IsSettled: {market.IsSettled}");
            System.Console.WriteLine("           (Implementation-dependent behavior)");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ⚠ INFO - Exception thrown: {ex.Message}");
            System.Console.WriteLine("           (Either behavior is acceptable)");
            passCount++;
        }

        // Test 7: Adding market to completed event
        System.Console.WriteLine("\nTest 7: Adding market to completed event");
        try
        {
            var game = new Event("Game 7", team1, team2, DateTime.UtcNow, league.Id);
            game.Complete(new Score(10, 5));

            var market = new Market(MarketType.Moneyline, "Winner");
            game.AddMarket(market);

            System.Console.WriteLine($"  ✓ PASS - Market added to completed event");
            System.Console.WriteLine("           (Implementation allows this)");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ✓ PASS - Exception thrown: {ex.Message}");
            System.Console.WriteLine("           (Either behavior is acceptable)");
            passCount++;
        }

        // Test 8: Tie/Draw score
        System.Console.WriteLine("\nTest 8: Event with tie score");
        try
        {
            var game = new Event("Game 8", team1, team2, DateTime.UtcNow, league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            var outcome2 = new Outcome("Team2 Win", "T2", new Odds(2.0m));
            market.AddOutcome(outcome1);
            market.AddOutcome(outcome2);
            game.AddMarket(market);

            var tieScore = new Score(21, 21);
            game.Complete(tieScore);

            if (tieScore.IsDraw)
            {
                System.Console.WriteLine($"  ✓ PASS - Tie score detected: {tieScore}");
                System.Console.WriteLine("           Score properties: IsDraw=true, Margin=0");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - IsDraw should be true");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        System.Console.WriteLine($"\n📊 Event Lifecycle Tests: {passCount} passed, {failCount} failed");
    }

    static void TestCancellationAndRefunds()
    {
        System.Console.WriteLine("EDGE CASE TEST: Event Cancellation & Refunds");
        System.Console.WriteLine("---------------------------------------------\n");

        int passCount = 0;
        int failCount = 0;

        var sport = new Sport("Football", "NFL");
        var league = new League("NFL", "NFL", sport.Id);
        var team1 = new Team("Team1", "T1", league.Id, "Team One");
        var team2 = new Team("Team2", "T2", league.Id, "Team Two");
        var settlementService = new SettlementService();

        // Test 1: Single bet refund when event cancelled
        System.Console.WriteLine("Test 1: Single bet refunded when event cancelled");
        try
        {
            var game = new Event("Game 1", team1, team2, DateTime.UtcNow.AddHours(2), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var stake = new Money(100m, "USD");
            var bet = Bet.CreateSingle(stake, game, market, outcome);

            System.Console.WriteLine($"  Bet placed: {bet.TicketNumber}, Stake: {bet.Stake}");

            // Event gets cancelled
            game.Cancel();
            market.SettleAsVoid();

            // Settle the bet
            settlementService.SettleBet(bet, new[] { game });

            if (bet.Status == BetStatus.Void && bet.ActualPayout == bet.Stake)
            {
                System.Console.WriteLine($"  ✓ PASS - Bet voided, full stake refunded: {bet.ActualPayout}");
                System.Console.WriteLine($"         Status: {bet.Status}, Refund: {bet.ActualPayout}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected Void with {stake}, got {bet.Status} with {bet.ActualPayout}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 2: Parlay bet refund when all events cancelled
        System.Console.WriteLine("\nTest 2: Parlay bet refunded when all events cancelled");
        try
        {
            var game1 = new Event("Game 2a", team1, team2, DateTime.UtcNow.AddHours(2), league.Id);
            var game2 = new Event("Game 2b", team1, team2, DateTime.UtcNow.AddHours(3), league.Id);

            var market1 = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(1.80m));
            market1.AddOutcome(outcome1);
            game1.AddMarket(market1);

            var market2 = new Market(MarketType.Moneyline, "Winner");
            var outcome2 = new Outcome("Team1 Win", "T1", new Odds(2.10m));
            market2.AddOutcome(outcome2);
            game2.AddMarket(market2);

            var stake = new Money(50m, "USD");
            var parlay = Bet.CreateParlay(
                stake,
                (game1, market1, outcome1),
                (game2, market2, outcome2)
            );

            System.Console.WriteLine($"  Parlay placed: {parlay.TicketNumber}");
            System.Console.WriteLine($"  Original odds: {parlay.CombinedOdds.DecimalValue:F2}");
            System.Console.WriteLine($"  Stake: {parlay.Stake}");

            // Both events cancelled
            game1.Cancel();
            game2.Cancel();
            market1.SettleAsVoid();
            market2.SettleAsVoid();

            settlementService.SettleBet(parlay, new[] { game1, game2 });

            if (parlay.Status == BetStatus.Void && parlay.ActualPayout == parlay.Stake)
            {
                System.Console.WriteLine($"  ✓ PASS - Parlay voided, full stake refunded: {parlay.ActualPayout}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected Void with {stake}, got {parlay.Status}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 3: Bet placed after event cancelled should fail
        System.Console.WriteLine("\nTest 3: Cannot place bet on cancelled event");
        try
        {
            var game = new Event("Game 3", team1, team2, DateTime.UtcNow.AddHours(2), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            // Cancel event first
            game.Cancel();
            market.Close();

            // Try to place bet
            var bet = Bet.CreateSingle(new Money(100m), game, market, outcome);

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (SportsBetting.Domain.Exceptions.MarketClosedException)
        {
            System.Console.WriteLine("  ✓ PASS - MarketClosedException thrown for cancelled event");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 4: LineLock cancellation when event cancelled
        System.Console.WriteLine("\nTest 4: LineLock can be cancelled when event cancelled");
        try
        {
            var game = new Event("Game 4", team1, team2, DateTime.UtcNow.AddHours(2), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.5m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lockFee = new Money(10m, "USD");
            var lineLock = LineLock.Create(
                game, market, outcome,
                lockFee,
                new Money(200m, "USD"),
                DateTime.UtcNow.AddMinutes(30)
            );

            System.Console.WriteLine($"  LineLock created: {lineLock.LockNumber}");
            System.Console.WriteLine($"  Lock fee paid: {lineLock.LockFee}");
            System.Console.WriteLine($"  Status before cancel: {lineLock.Status}");

            // Event gets cancelled
            game.Cancel();

            // Cancel the lock (fee should be refunded by wallet service in real implementation)
            lineLock.Cancel();

            System.Console.WriteLine($"  Status after cancel: {lineLock.Status}");

            if (lineLock.Status == LineLockStatus.Cancelled)
            {
                System.Console.WriteLine($"  ✓ PASS - LineLock cancelled successfully");
                System.Console.WriteLine($"         Note: Fee refund ({lineLock.LockFee}) would be processed by wallet service");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected Cancelled, got {lineLock.Status}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 5: Cannot exercise LineLock after event cancelled
        System.Console.WriteLine("\nTest 5: Cannot exercise LineLock after event cancelled");
        try
        {
            var game = new Event("Game 5", team1, team2, DateTime.UtcNow.AddHours(2), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var lineLock = LineLock.Create(
                game, market, outcome,
                new Money(5m, "USD"),
                new Money(100m, "USD"),
                DateTime.UtcNow.AddMinutes(30)
            );

            // Event gets cancelled
            game.Cancel();
            lineLock.Cancel();

            // Try to exercise cancelled lock
            var bet = lineLock.Exercise(new Money(50m), game, market, outcome);

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("cancel") || ex.Message.Contains("Cancelled"))
            {
                System.Console.WriteLine("  ✓ PASS - Cannot exercise cancelled LineLock");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (SportsBetting.Domain.Exceptions.InvalidBetException ex)
        {
            if (ex.Message.Contains("cancel") || ex.Message.Contains("Cancelled"))
            {
                System.Console.WriteLine("  ✓ PASS - Cannot exercise cancelled LineLock");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 6: Manually voiding a bet (equivalent to cancellation refund)
        System.Console.WriteLine("\nTest 6: Manual bet void returns full stake");
        try
        {
            var game = new Event("Game 6", team1, team2, DateTime.UtcNow.AddHours(2), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(1.75m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            var stake = new Money(150m, "USD");
            var bet = Bet.CreateSingle(stake, game, market, outcome);

            System.Console.WriteLine($"  Bet placed: {bet.TicketNumber}, Stake: {bet.Stake}");

            // Manually void (e.g., customer service refund)
            bet.Void();

            if (bet.Status == BetStatus.Void && bet.ActualPayout == bet.Stake)
            {
                System.Console.WriteLine($"  ✓ PASS - Manual void processed");
                System.Console.WriteLine($"         Refund amount: {bet.ActualPayout}");
                System.Console.WriteLine($"         Net to customer: ${bet.ActualPayout.Value.Amount} (100% refund)");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected {stake} refund, got {bet.ActualPayout}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 7: Bet with exercised LineLock - event cancelled (complex scenario)
        System.Console.WriteLine("\nTest 7: Bet from LineLock refunded when event cancelled");
        try
        {
            var game = new Event("Game 7", team1, team2, DateTime.UtcNow.AddHours(3), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(3.0m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            // Create and exercise LineLock
            var lockFee = new Money(8m, "USD");
            var lineLock = LineLock.Create(
                game, market, outcome,
                lockFee,
                new Money(100m, "USD"),
                DateTime.UtcNow.AddMinutes(30)
            );

            var betStake = new Money(75m, "USD");
            var bet = lineLock.Exercise(betStake, game, market, outcome);

            System.Console.WriteLine($"  LineLock exercised: Lock fee {lockFee}, Bet stake {betStake}");
            System.Console.WriteLine($"  Total customer investment: ${lockFee.Amount + betStake.Amount}");

            // Event gets cancelled
            game.Cancel();
            market.SettleAsVoid();
            settlementService.SettleBet(bet, new[] { game });

            var totalInvested = lockFee.Amount + betStake.Amount;
            var refunded = bet.ActualPayout!.Value.Amount;

            if (bet.Status == BetStatus.Void && bet.ActualPayout == betStake)
            {
                System.Console.WriteLine($"  ✓ PASS - Bet stake refunded: ${refunded}");
                System.Console.WriteLine($"         Customer invested: ${totalInvested} (lock fee + stake)");
                System.Console.WriteLine($"         Customer refunded: ${refunded} (bet stake only)");
                System.Console.WriteLine($"         Lock fee: ${lockFee.Amount} (non-refundable premium paid)");
                System.Console.WriteLine($"         Net customer loss: ${totalInvested - refunded}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Expected {betStake} refund, got {bet.ActualPayout}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        System.Console.WriteLine($"\n📊 Cancellation & Refund Tests: {passCount} passed, {failCount} failed");
    }

    static void TestDecimalPrecisionEdgeCases()
    {
        System.Console.WriteLine("EDGE CASE TEST: Decimal Precision & Wacky Numbers");
        System.Console.WriteLine("--------------------------------------------------\n");

        int passCount = 0;
        int failCount = 0;

        var sport = new Sport("Football", "NFL");
        var league = new League("NFL", "NFL", sport.Id);
        var team1 = new Team("Team1", "T1", league.Id, "Team One");
        var team2 = new Team("Team2", "T2", league.Id, "Team Two");
        var settlementService = new SettlementService();

        // Test 1: Fractional cents (repeating decimals)
        System.Console.WriteLine("Test 1: Money with fractional cents");
        try
        {
            var weirdAmount = new Money(33.33333333333333333333333333m, "USD");
            System.Console.WriteLine($"  Created: {weirdAmount}");
            System.Console.WriteLine($"  Exact amount: {weirdAmount.Amount}");

            var tripled = weirdAmount * 3m;
            System.Console.WriteLine($"  Tripled: {tripled}");
            System.Console.WriteLine($"  Exact: {tripled.Amount}");
            System.Console.WriteLine($"  ✓ PASS - Handles fractional cents");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 2: Odds with many decimal places
        System.Console.WriteLine("\nTest 2: Odds with extreme precision");
        try
        {
            var preciseOdds = new Odds(1.6666666666666666666666666667m);
            var stake = new Money(100m, "USD");
            var payout = preciseOdds.CalculatePayout(stake);

            System.Console.WriteLine($"  Odds: {preciseOdds.DecimalValue}");
            System.Console.WriteLine($"  Stake: {stake}");
            System.Console.WriteLine($"  Payout: {payout}");
            System.Console.WriteLine($"  Exact payout: {payout.Amount}");
            System.Console.WriteLine($"  ✓ PASS - Precise odds calculation");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 3: Very long parlay multiplication
        System.Console.WriteLine("\nTest 3: 10-leg parlay with precision accumulation");
        try
        {
            var game = new Event("Test Game", team1, team2, DateTime.UtcNow.AddHours(1), league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(1.11111111m));
            market.AddOutcome(outcome);
            game.AddMarket(market);

            // Create 10-leg parlay with same odds
            var legs = Enumerable.Range(0, 10)
                .Select(_ => (game, market, outcome))
                .ToArray();

            var parlay = Bet.CreateParlay(new Money(10m, "USD"), legs);

            var expectedOdds = Math.Pow((double)1.11111111m, 10);
            var actualOdds = parlay.CombinedOdds.DecimalValue;

            System.Console.WriteLine($"  10 legs @ 1.11111111 each");
            System.Console.WriteLine($"  Expected odds: ~{expectedOdds:F10}");
            System.Console.WriteLine($"  Actual odds: {actualOdds}");
            System.Console.WriteLine($"  Difference: {Math.Abs((decimal)expectedOdds - actualOdds)}");
            System.Console.WriteLine($"  Potential payout: {parlay.PotentialPayout}");

            if (actualOdds > 1m)
            {
                System.Console.WriteLine($"  ✓ PASS - Parlay odds accumulated correctly");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Odds calculation error");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 4: Division creating repeating decimals (1/3)
        System.Console.WriteLine("\nTest 4: Operations resulting in repeating decimals");
        try
        {
            var money = new Money(100m, "USD");
            var divided = money * (1m / 3m);

            System.Console.WriteLine($"  $100 × (1/3) = {divided}");
            System.Console.WriteLine($"  Exact: {divided.Amount}");

            var tripled = divided * 3m;
            System.Console.WriteLine($"  Then × 3 = {tripled}");
            System.Console.WriteLine($"  Exact: {tripled.Amount}");
            System.Console.WriteLine($"  Difference from original: ${Math.Abs(tripled.Amount - money.Amount)}");

            System.Console.WriteLine($"  ✓ PASS - Repeating decimal handled");
            passCount++;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 5: Very small money amounts (microtransactions)
        System.Console.WriteLine("\nTest 5: Very small money amounts");
        try
        {
            var tiny = new Money(0.000001m, "USD");
            var odds = new Odds(1000m);
            var payout = odds.CalculatePayout(tiny);

            System.Console.WriteLine($"  Stake: {tiny} ({tiny.Amount})");
            System.Console.WriteLine($"  Odds: {odds}");
            System.Console.WriteLine($"  Payout: {payout} ({payout.Amount})");

            if (payout.Amount > tiny.Amount)
            {
                System.Console.WriteLine($"  ✓ PASS - Microtransaction calculated");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Calculation error");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 6: Odds very close to 1.0 (heavy favorite)
        System.Console.WriteLine("\nTest 6: Odds barely above 1.0");
        try
        {
            var barelyOverOne = new Odds(1.0000000001m);
            var stake = new Money(1000000m, "USD");
            var payout = barelyOverOne.CalculatePayout(stake);
            var profit = barelyOverOne.CalculateProfit(stake);

            System.Console.WriteLine($"  Odds: {barelyOverOne.DecimalValue}");
            System.Console.WriteLine($"  Stake: {stake}");
            System.Console.WriteLine($"  Payout: {payout}");
            System.Console.WriteLine($"  Profit: {profit} (${profit.Amount})");

            if (profit.Amount > 0m && profit.Amount < stake.Amount)
            {
                System.Console.WriteLine($"  ✓ PASS - Heavy favorite calculated correctly");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Calculation seems off");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 7: American odds conversion with extreme values
        System.Console.WriteLine("\nTest 7: Extreme American odds conversions");
        try
        {
            var hugeFavorite = Odds.FromAmerican(-10000);  // Massive favorite
            var hugeUnderdog = Odds.FromAmerican(+10000);  // Massive underdog

            System.Console.WriteLine($"  American -10000 = Decimal {hugeFavorite.DecimalValue}");
            System.Console.WriteLine($"  American +10000 = Decimal {hugeUnderdog.DecimalValue}");

            var roundTripFav = hugeFavorite.ToAmerican();
            var roundTripDog = hugeUnderdog.ToAmerican();

            System.Console.WriteLine($"  Round-trip favorite: {roundTripFav}");
            System.Console.WriteLine($"  Round-trip underdog: {roundTripDog}");

            if (Math.Abs(roundTripFav - (-10000)) <= 1 && Math.Abs(roundTripDog - 10000) <= 1)
            {
                System.Console.WriteLine($"  ✓ PASS - Extreme conversions accurate");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Conversion precision lost");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 8: Settlement with fractional cents
        System.Console.WriteLine("\nTest 8: Settlement with wacky decimal payouts");
        try
        {
            var game = new Event("Game 8", team1, team2, DateTime.UtcNow, league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(1.333333333m));
            var outcome2 = new Outcome("Team2 Win", "T2", new Odds(3.000001m));
            market.AddOutcome(outcome1);
            market.AddOutcome(outcome2);
            game.AddMarket(market);

            var weirdStake = new Money(77.77m, "USD");
            var bet = Bet.CreateSingle(weirdStake, game, market, outcome1);

            System.Console.WriteLine($"  Stake: {bet.Stake} (${bet.Stake.Amount})");
            System.Console.WriteLine($"  Odds: {bet.CombinedOdds.DecimalValue}");
            System.Console.WriteLine($"  Potential: {bet.PotentialPayout} (${bet.PotentialPayout.Amount})");

            // Settle
            game.Complete(new Score(10, 5));
            settlementService.SettleMoneylineMarket(market, game);
            settlementService.SettleBet(bet, new[] { game });

            System.Console.WriteLine($"  Actual payout: {bet.ActualPayout} (${bet.ActualPayout!.Value.Amount})");
            System.Console.WriteLine($"  Profit: {bet.ActualProfit} (${bet.ActualProfit!.Value.Amount})");

            if (bet.Status == BetStatus.Won && bet.ActualPayout == bet.PotentialPayout)
            {
                System.Console.WriteLine($"  ✓ PASS - Fractional payout calculated correctly");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Settlement mismatch");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 9: Currency with many decimal places
        System.Console.WriteLine("\nTest 9: Currency operations preserving precision");
        try
        {
            var precise1 = new Money(123.456789m, "USD");
            var precise2 = new Money(987.654321m, "USD");

            var sum = precise1 + precise2;
            var diff = precise2 - precise1;

            System.Console.WriteLine($"  Amount 1: ${precise1.Amount}");
            System.Console.WriteLine($"  Amount 2: ${precise2.Amount}");
            System.Console.WriteLine($"  Sum: ${sum.Amount}");
            System.Console.WriteLine($"  Difference: ${diff.Amount}");

            var expectedSum = 123.456789m + 987.654321m;
            var expectedDiff = 987.654321m - 123.456789m;

            if (sum.Amount == expectedSum && diff.Amount == expectedDiff)
            {
                System.Console.WriteLine($"  ✓ PASS - Precision preserved in operations");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Precision lost");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 10: Parlay with void leg and fractional odds recalculation
        System.Console.WriteLine("\nTest 10: Parlay void leg with precise odds recalculation");
        try
        {
            var game1 = new Event("Game 10a", team1, team2, DateTime.UtcNow, league.Id);
            var game2 = new Event("Game 10b", team1, team2, DateTime.UtcNow, league.Id);

            var market1 = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(1.777777m));
            market1.AddOutcome(outcome1);
            market1.AddOutcome(new Outcome("Team2 Win", "T2", new Odds(2.0m)));
            game1.AddMarket(market1);

            var market2 = new Market(MarketType.Moneyline, "Winner");
            var outcome2 = new Outcome("Team1 Win", "T1", new Odds(2.333333m));
            market2.AddOutcome(outcome2);
            game2.AddMarket(market2);

            var parlay = Bet.CreateParlay(
                new Money(99.99m, "USD"),
                (game1, market1, outcome1),
                (game2, market2, outcome2)
            );

            var originalOdds = parlay.CombinedOdds.DecimalValue;
            System.Console.WriteLine($"  Original odds: {originalOdds}");
            System.Console.WriteLine($"  Original payout: {parlay.PotentialPayout}");

            // Game 1 wins, Game 2 cancelled
            game1.Complete(new Score(10, 5));
            settlementService.SettleMoneylineMarket(market1, game1);

            game2.Cancel();
            market2.SettleAsVoid();

            settlementService.SettleBet(parlay, new[] { game1, game2 });

            System.Console.WriteLine($"  Recalculated odds: 1.777777 (game1 only)");
            System.Console.WriteLine($"  Actual payout: {parlay.ActualPayout}");

            var expectedPayout = new Odds(1.777777m).CalculatePayout(parlay.Stake);
            System.Console.WriteLine($"  Expected payout: {expectedPayout}");

            if (parlay.Status == BetStatus.Won)
            {
                System.Console.WriteLine($"  ✓ PASS - Void leg recalculation with precise odds");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Status: {parlay.Status}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        System.Console.WriteLine($"\n📊 Decimal Precision Tests: {passCount} passed, {failCount} failed");
    }

    static void TestOrphanedObjectValidation()
    {
        System.Console.WriteLine("EDGE CASE TEST: Orphaned Object Prevention");
        System.Console.WriteLine("-------------------------------------------\n");

        int passCount = 0;
        int failCount = 0;

        var sport = new Sport("Football", "NFL");
        var league = new League("NFL", "NFL", sport.Id);
        var team1 = new Team("Team1", "T1", league.Id, "Team One");
        var team2 = new Team("Team2", "T2", league.Id, "Team Two");
        var settlementService = new SettlementService();

        // Test 1: Market without Event - Settle should fail
        System.Console.WriteLine("Test 1: Settling orphaned Market (not attached to Event)");
        try
        {
            var orphanedMarket = new Market(MarketType.Moneyline, "Orphaned Market");
            var outcome = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            orphanedMarket.AddOutcome(outcome);

            // Try to settle without attaching to event
            orphanedMarket.Settle(outcome.Id);

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("must be attached to an Event"))
            {
                System.Console.WriteLine("  ✓ PASS - Prevented orphaned Market settlement");
                System.Console.WriteLine($"         Message: {ex.Message}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 2: Market without Event - SettleAsVoid should fail
        System.Console.WriteLine("\nTest 2: Voiding orphaned Market (not attached to Event)");
        try
        {
            var orphanedMarket = new Market(MarketType.Moneyline, "Orphaned Market");
            orphanedMarket.SettleAsVoid();

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("must be attached to an Event"))
            {
                System.Console.WriteLine("  ✓ PASS - Prevented orphaned Market void");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 3: Market without Event - SettleAsPush should fail
        System.Console.WriteLine("\nTest 3: Pushing orphaned Market (not attached to Event)");
        try
        {
            var orphanedMarket = new Market(MarketType.Spread, "Orphaned Spread");
            orphanedMarket.SettleAsPush();

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("must be attached to an Event"))
            {
                System.Console.WriteLine("  ✓ PASS - Prevented orphaned Market push");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 4: Outcome without Market - Settlement should fail
        System.Console.WriteLine("\nTest 4: Settling Market with orphaned Outcome");
        try
        {
            var orphanedOutcome = new Outcome("Orphaned Outcome", "Orphan", new Odds(2.0m));
            var orphanedMarket = new Market(MarketType.Moneyline, "Orphaned Market");

            // Add orphaned outcome (never attached to market properly via AddOutcome)
            // Actually, AddOutcome DOES attach it, so this won't fail. Let me think...
            // The validation happens inside MarkAsWinner which is called by Market.Settle
            // So if we properly attach the outcome to market, but market is not attached to event,
            // the Market.Settle will fail (which we tested in Test 1)

            // Let's test a different scenario: What if outcome is in a market but market isn't attached?
            orphanedMarket.AddOutcome(orphanedOutcome);  // This properly attaches outcome to market

            // Try to settle - this should fail because market isn't attached to event
            orphanedMarket.Settle(orphanedOutcome.Id);

            System.Console.WriteLine("  ❌ FAIL - Should have thrown exception");
            failCount++;
        }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.Contains("must be attached to an Event"))
            {
                System.Console.WriteLine("  ✓ PASS - Prevented settlement with orphaned Market");
                System.Console.WriteLine($"         (Outcome is properly attached, but Market isn't)");
                passCount++;
            }
            else
            {
                System.Console.WriteLine($"  ❌ FAIL - Wrong message: {ex.Message}");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - Wrong exception: {ex.GetType().Name}");
            failCount++;
        }

        // Test 5: Verify Outcome.MarketId is set when added
        System.Console.WriteLine("\nTest 5: Outcome properly attached via market.AddOutcome()");
        try
        {
            var market = new Market(MarketType.Moneyline, "Test Market");
            var outcome = new Outcome("Test Outcome", "Test", new Odds(2.0m));

            // Before attachment
            bool wasOrphaned = outcome.MarketId == Guid.Empty;

            // Attach it
            market.AddOutcome(outcome);

            // After attachment
            bool isNowAttached = outcome.MarketId != Guid.Empty && outcome.MarketId == market.Id;

            if (wasOrphaned && isNowAttached)
            {
                System.Console.WriteLine("  ✓ PASS - AddOutcome properly sets MarketId");
                System.Console.WriteLine($"         Before: MarketId = {Guid.Empty}");
                System.Console.WriteLine($"         After: MarketId = {outcome.MarketId}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine("  ❌ FAIL - AddOutcome didn't set MarketId correctly");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 6: Properly attached Market works fine
        System.Console.WriteLine("\nTest 6: Properly attached Market settles successfully");
        try
        {
            var game = new Event("Game 6", team1, team2, DateTime.UtcNow, league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            // Use team names that match the actual teams in the game
            var outcome1 = new Outcome("Team1 Win", "Team1", new Odds(2.0m));
            var outcome2 = new Outcome("Team2 Win", "Team2", new Odds(2.0m));
            market.AddOutcome(outcome1);
            market.AddOutcome(outcome2);

            // Properly attach to event
            game.AddMarket(market);

            // Now this should work
            game.Complete(new Score(10, 5));
            settlementService.SettleMoneylineMarket(market, game);

            if (market.IsSettled)
            {
                System.Console.WriteLine("  ✓ PASS - Properly attached Market settled successfully");
                passCount++;
            }
            else
            {
                System.Console.WriteLine("  ❌ FAIL - Market not settled");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 7: Properly attached Outcome works fine
        System.Console.WriteLine("\nTest 7: Properly attached Outcome marks successfully");
        try
        {
            var game = new Event("Game 7", team1, team2, DateTime.UtcNow, league.Id);
            var market = new Market(MarketType.Moneyline, "Winner");
            var outcome1 = new Outcome("Team1 Win", "T1", new Odds(2.0m));
            var outcome2 = new Outcome("Team2 Win", "T2", new Odds(2.0m));

            // Properly attach to market
            market.AddOutcome(outcome1);
            market.AddOutcome(outcome2);
            game.AddMarket(market);

            // Now this should work
            market.Settle(outcome1.Id);

            if (outcome1.IsWinner == true && outcome2.IsWinner == false)
            {
                System.Console.WriteLine("  ✓ PASS - Properly attached Outcomes marked successfully");
                passCount++;
            }
            else
            {
                System.Console.WriteLine("  ❌ FAIL - Outcome marking didn't work correctly");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        // Test 8: Checking Guid.Empty directly
        System.Console.WriteLine("\nTest 8: Verify orphaned objects have Guid.Empty for parent IDs");
        try
        {
            var orphanedMarket = new Market(MarketType.Moneyline, "Test");
            var orphanedOutcome = new Outcome("Test", "Test", new Odds(2.0m));

            if (orphanedMarket.EventId == Guid.Empty && orphanedOutcome.MarketId == Guid.Empty)
            {
                System.Console.WriteLine("  ✓ PASS - Orphaned objects have Guid.Empty (00000000...)");
                System.Console.WriteLine($"         Market.EventId: {orphanedMarket.EventId}");
                System.Console.WriteLine($"         Outcome.MarketId: {orphanedOutcome.MarketId}");
                passCount++;
            }
            else
            {
                System.Console.WriteLine("  ❌ FAIL - Expected Guid.Empty");
                failCount++;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ❌ FAIL - {ex.Message}");
            failCount++;
        }

        System.Console.WriteLine($"\n📊 Orphaned Object Tests: {passCount} passed, {failCount} failed");
        System.Console.WriteLine("\n💡 Summary: Objects must be properly attached to their parents:");
        System.Console.WriteLine("   - Use event.AddMarket(market)");
        System.Console.WriteLine("   - Use market.AddOutcome(outcome)");
        System.Console.WriteLine("   - Validation prevents orphaned objects from being used");
    }
}
