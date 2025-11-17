using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Exceptions;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Services;

/// <summary>
/// Service for settling bets based on event outcomes
/// </summary>
public class SettlementService
{
    private readonly ICommissionService _commissionService;

    public SettlementService(ICommissionService commissionService)
    {
        _commissionService = commissionService;
    }
    /// <summary>
    /// Settle a moneyline market based on final score
    /// </summary>
    public void SettleMoneylineMarket(Market market, Event evt)
    {
        if (market.Type != MarketType.Moneyline)
            throw new ArgumentException("Market must be Moneyline type", nameof(market));

        if (!evt.FinalScore.HasValue)
            throw new SettlementException("Event does not have a final score");

        var score = evt.FinalScore.Value;

        // Find home and away outcomes
        var homeOutcome = market.Outcomes.FirstOrDefault(o => o.Name.Contains(evt.HomeTeam.Name, StringComparison.OrdinalIgnoreCase));
        var awayOutcome = market.Outcomes.FirstOrDefault(o => o.Name.Contains(evt.AwayTeam.Name, StringComparison.OrdinalIgnoreCase));

        if (homeOutcome == null || awayOutcome == null)
            throw new SettlementException("Cannot find home/away outcomes in moneyline market");

        Guid winningOutcomeId;

        if (score.IsHomeWin)
        {
            winningOutcomeId = homeOutcome.Id;
        }
        else if (score.IsAwayWin)
        {
            winningOutcomeId = awayOutcome.Id;
        }
        else
        {
            // Draw - check if there's a draw outcome
            var drawOutcome = market.Outcomes.FirstOrDefault(o => o.Name.Contains("Draw", StringComparison.OrdinalIgnoreCase));
            if (drawOutcome != null)
            {
                winningOutcomeId = drawOutcome.Id;
            }
            else
            {
                // No draw outcome, void the market (push)
                market.VoidOutcomes(homeOutcome.Id, awayOutcome.Id);
                return;
            }
        }

        market.Settle(winningOutcomeId);
    }

    /// <summary>
    /// Settle a spread market based on final score
    /// </summary>
    public void SettleSpreadMarket(Market market, Event evt)
    {
        if (market.Type != MarketType.Spread)
            throw new ArgumentException("Market must be Spread type", nameof(market));

        if (!evt.FinalScore.HasValue)
            throw new SettlementException("Event does not have a final score");

        var score = evt.FinalScore.Value;

        // Spread markets have 2 outcomes with lines
        if (market.Outcomes.Count != 2)
            throw new SettlementException("Spread market must have exactly 2 outcomes");

        foreach (var outcome in market.Outcomes)
        {
            if (!outcome.Line.HasValue)
                throw new SettlementException("Spread outcome must have a line value");
        }

        // Determine which outcome is home and which is away based on the line
        var homeOutcome = market.Outcomes.FirstOrDefault(o => o.Name.Contains(evt.HomeTeam.Name, StringComparison.OrdinalIgnoreCase));
        var awayOutcome = market.Outcomes.FirstOrDefault(o => o.Name.Contains(evt.AwayTeam.Name, StringComparison.OrdinalIgnoreCase));

        if (homeOutcome == null || awayOutcome == null)
            throw new SettlementException("Cannot determine home/away outcomes");

        // Apply the spread
        var homeSpread = homeOutcome.Line!.Value;
        var adjustedHomeScore = score.HomeScore + homeSpread;

        if (adjustedHomeScore > score.AwayScore)
        {
            // Home covers
            market.Settle(homeOutcome.Id);
        }
        else if (adjustedHomeScore < score.AwayScore)
        {
            // Away covers
            market.Settle(awayOutcome.Id);
        }
        else
        {
            // Push - settle market as push (tie on the line)
            market.SettleAsPush();
        }
    }

    /// <summary>
    /// Settle a totals (over/under) market based on final score
    /// </summary>
    public void SettleTotalsMarket(Market market, Event evt)
    {
        if (market.Type != MarketType.Totals)
            throw new ArgumentException("Market must be Totals type", nameof(market));

        if (!evt.FinalScore.HasValue)
            throw new SettlementException("Event does not have a final score");

        var score = evt.FinalScore.Value;
        var totalPoints = score.TotalPoints;

        var overOutcome = market.Outcomes.FirstOrDefault(o => o.Name.Contains("Over", StringComparison.OrdinalIgnoreCase));
        var underOutcome = market.Outcomes.FirstOrDefault(o => o.Name.Contains("Under", StringComparison.OrdinalIgnoreCase));

        if (overOutcome == null || underOutcome == null)
            throw new SettlementException("Cannot find over/under outcomes");

        if (!overOutcome.Line.HasValue)
            throw new SettlementException("Totals outcome must have a line value");

        var line = overOutcome.Line.Value;

        if (totalPoints > line)
        {
            // Over wins
            market.Settle(overOutcome.Id);
        }
        else if (totalPoints < line)
        {
            // Under wins
            market.Settle(underOutcome.Id);
        }
        else
        {
            // Push - exact total, settle market as push
            market.SettleAsPush();
        }
    }

    /// <summary>
    /// Settle all markets for an event
    /// </summary>
    public void SettleEvent(Event evt)
    {
        if (!evt.FinalScore.HasValue)
            throw new SettlementException("Event must have a final score to settle");

        foreach (var market in evt.Markets)
        {
            if (market.IsSettled)
                continue;

            switch (market.Type)
            {
                case MarketType.Moneyline:
                    SettleMoneylineMarket(market, evt);
                    break;

                case MarketType.Spread:
                    SettleSpreadMarket(market, evt);
                    break;

                case MarketType.Totals:
                    SettleTotalsMarket(market, evt);
                    break;

                default:
                    // Other market types would need custom settlement logic
                    break;
            }
        }
    }

    /// <summary>
    /// Settle a bet based on its selections and the current state of markets
    /// </summary>
    public void SettleBet(Bet bet, IEnumerable<Event> events)
    {
        // Check if this is an exchange bet - those are settled differently
        if (bet.BetMode == BetMode.Exchange)
        {
            throw new InvalidOperationException(
                "Exchange bets are settled via match settlement. " +
                "Use SettleExchangeMatch() instead.");
        }

        // First, settle all selections
        foreach (var selection in bet.Selections)
        {
            var evt = events.FirstOrDefault(e => e.Id == selection.EventId);
            if (evt == null)
                throw new SettlementException($"Event {selection.EventId} not found");

            var market = evt.GetMarket(selection.MarketId);
            if (market == null)
                throw new SettlementException($"Market {selection.MarketId} not found");

            if (!market.IsSettled)
                throw new SettlementException($"Market {market.Name} is not yet settled");

            var outcome = market.GetOutcome(selection.OutcomeId);
            if (outcome == null)
                throw new SettlementException($"Outcome {selection.OutcomeId} not found");

            selection.Settle(outcome);
        }

        // Now settle the bet based on its selections
        bet.Settle();
    }

    /// <summary>
    /// Settle an exchange match (P2P bet pair)
    /// Determines winner, calculates commission based on tier and liquidity role, and distributes winnings
    /// </summary>
    /// <param name="match">The matched pair of bets to settle</param>
    /// <param name="outcome">The outcome that determines the winner</param>
    /// <param name="backUser">The back bet user (with Statistics loaded)</param>
    /// <param name="layUser">The lay bet user (with Statistics loaded)</param>
    /// <returns>The winner's bet and net winnings</returns>
    public (Bet winnerBet, Money netWinnings) SettleExchangeMatch(
        BetMatch match,
        Outcome outcome,
        User backUser,
        User layUser)
    {
        if (match.IsSettled)
        {
            throw new InvalidOperationException("Match is already settled");
        }

        if (!outcome.IsWinner.HasValue || !outcome.IsWinner.Value)
        {
            throw new InvalidOperationException("Outcome must be marked as winner to settle match");
        }

        // Determine if back bet wins (outcome occurred) or lay bet wins (outcome didn't occur)
        bool backBetWins = outcome.IsWinner.Value;

        // Get winner and loser
        var winnerUser = backBetWins ? backUser : layUser;
        var winnerExchangeBet = backBetWins ? match.BackBet : match.LayBet;
        var loserExchangeBet = backBetWins ? match.LayBet : match.BackBet;
        var winnerBet = winnerExchangeBet.Bet;
        var loserBet = loserExchangeBet.Bet;

        // Calculate gross winnings
        var grossWinnings = match.CalculateGrossWinnings();

        // Get winner's liquidity role
        var winnerRole = match.GetLiquidityRole(winnerExchangeBet.Id);

        // Calculate commission based on winner's tier and role
        var commission = _commissionService.CalculateCommission(winnerUser, grossWinnings, winnerRole);
        var netWinnings = grossWinnings - commission;

        // Get the currency from the winner's stake
        var currency = winnerBet.Stake.Currency;

        // Settle the match entity with commission
        var backCommission = backBetWins ? commission : 0;
        var layCommission = !backBetWins ? commission : 0;
        match.Settle(backBetWins, backCommission, layCommission);

        // Winner gets: their stake back + net winnings
        var totalPayout = new Money(match.MatchedStake + netWinnings, currency);

        // Update bet statuses
        winnerBet.SetStatus(BetStatus.Won);
        winnerBet.SetActualPayout(totalPayout);

        loserBet.SetStatus(BetStatus.Lost);
        loserBet.SetActualPayout(Money.Zero(currency));

        // Update winner's statistics
        if (winnerUser.Statistics != null)
        {
            winnerUser.Statistics.RecordCommissionPaid(commission);
            winnerUser.Statistics.RecordBetSettled(netWinnings);
        }

        // Update loser's statistics
        var loserUser = backBetWins ? layUser : backUser;
        if (loserUser.Statistics != null)
        {
            loserUser.Statistics.RecordBetSettled(-match.MatchedStake);
        }

        return (winnerBet, new Money(netWinnings, currency));
    }

    /// <summary>
    /// Settle all exchange matches for a specific outcome
    /// </summary>
    /// <param name="matches">The matches to settle (with BackBet.Bet.User and LayBet.Bet.User loaded with Statistics)</param>
    /// <param name="outcome">The outcome that determines winners</param>
    /// <returns>List of (winner bet, net winnings) for all settled matches</returns>
    public List<(Bet winnerBet, Money netWinnings)> SettleExchangeMatches(
        IEnumerable<BetMatch> matches,
        Outcome outcome)
    {
        var results = new List<(Bet, Money)>();

        foreach (var match in matches.Where(m => !m.IsSettled))
        {
            var backUser = match.BackBet.Bet.User;
            var layUser = match.LayBet.Bet.User;

            if (backUser == null || layUser == null)
            {
                throw new InvalidOperationException(
                    "Users must be loaded for match settlement. " +
                    "Include User navigation property on Bet entities.");
            }

            var result = SettleExchangeMatch(match, outcome, backUser, layUser);
            results.Add(result);
        }

        return results;
    }
}
