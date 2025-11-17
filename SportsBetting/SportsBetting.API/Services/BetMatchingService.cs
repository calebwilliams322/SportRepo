using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportsBetting.API.DTOs;
using SportsBetting.API.Hubs;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.API.Services;

/// <summary>
/// Service for matching exchange bets
/// Implements P2P bet matching logic with configurable strategies
/// </summary>
public class BetMatchingService : IBetMatchingService
{
    private readonly SportsBettingDbContext _context;
    private readonly ILogger<BetMatchingService> _logger;
    private readonly IMatchingStrategy _matchingStrategy;
    private readonly IHubContext<OrderBookHub> _hubContext;

    public BetMatchingService(
        SportsBettingDbContext context,
        ILogger<BetMatchingService> logger,
        IMatchingStrategy matchingStrategy,
        IHubContext<OrderBookHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _matchingStrategy = matchingStrategy;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Try to match a new exchange bet with existing unmatched bets
    /// Uses configurable matching strategy (FIFO, Pro-Rata, or Hybrid)
    /// </summary>
    public async Task<MatchResult> MatchBetAsync(ExchangeBet newBet)
    {
        var result = new MatchResult
        {
            MatchedAmount = 0,
            UnmatchedAmount = newBet.TotalStake
        };

        // Get outcome to find opposing bets
        var outcomeId = newBet.Bet.Selections.First().OutcomeId;

        // Find opposing bets
        var oppositeSide = newBet.Side == BetSide.Back ? BetSide.Lay : BetSide.Back;

        var candidates = await GetMatchingCandidatesAsync(
            outcomeId,
            oppositeSide,
            newBet.ProposedOdds,
            newBet.Side);

        if (candidates.Count == 0)
        {
            result.Message = "No matches found - bet is unmatched";
            await _context.SaveChangesAsync();
            return result;
        }

        // Use matching strategy to allocate matches
        var allocations = _matchingStrategy.AllocateMatches(newBet.TotalStake, candidates);

        // Create matches based on allocations
        foreach (var allocation in allocations)
        {
            var candidate = allocation.Key;
            var matchAmount = allocation.Value;

            if (matchAmount <= 0)
                continue;

            // Create the match
            // candidate is the maker (was in order book first)
            // newBet is the taker (just matched)
            var match = new BetMatch(
                backBet: newBet.Side == BetSide.Back ? newBet : candidate,
                layBet: newBet.Side == BetSide.Lay ? newBet : candidate,
                matchedStake: matchAmount,
                matchedOdds: newBet.ProposedOdds,
                makerBet: candidate  // Candidate was in order book first
            );

            _context.BetMatches.Add(match);

            // Update both bets
            newBet.ApplyMatch(matchAmount);
            candidate.ApplyMatch(matchAmount);

            result.Matches.Add(match);

            _logger.LogInformation(
                "Matched {Amount} between {NewBetId} and {CandidateId} at odds {Odds}",
                matchAmount, newBet.Id, candidate.Id, newBet.ProposedOdds);
        }

        result.MatchedAmount = allocations.Values.Sum();
        result.UnmatchedAmount = newBet.TotalStake - result.MatchedAmount;
        result.FullyMatched = result.UnmatchedAmount == 0;
        result.Message = result.FullyMatched
            ? "Bet fully matched"
            : result.MatchedAmount > 0
                ? $"Bet partially matched: {result.MatchedAmount:C} matched, {result.UnmatchedAmount:C} unmatched"
                : "No matches found - bet is unmatched";

        await _context.SaveChangesAsync();

        // Send WebSocket notifications
        if (result.Matches.Count > 0)
        {
            // Notify the new bet's user
            await SendBetMatchedNotification(newBet, result);

            // Notify each matched candidate's user
            foreach (var match in result.Matches)
            {
                var otherBet = match.BackBet.Id == newBet.Id ? match.LayBet : match.BackBet;
                await SendBetMatchedNotification(otherBet, new MatchResult
                {
                    MatchedAmount = match.MatchedStake,
                    UnmatchedAmount = otherBet.UnmatchedStake,
                    FullyMatched = otherBet.State == BetState.Matched,
                    Matches = new List<BetMatch> { match }
                });
            }

            // Broadcast updated order book to all watchers
            await BroadcastOrderBookUpdate(outcomeId);
        }

        return result;
    }

    /// <summary>
    /// Get matching candidates for a bet
    /// Applies FIFO logic: best odds first, then earliest timestamp
    /// </summary>
    private async Task<List<ExchangeBet>> GetMatchingCandidatesAsync(
        Guid outcomeId,
        BetSide oppositeSide,
        decimal proposedOdds,
        BetSide originalSide)
    {
        var query = _context.ExchangeBets
            .Include(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
            .Where(eb => eb.Bet.Selections.Any(s => s.OutcomeId == outcomeId))
            .Where(eb => eb.Side == oppositeSide)
            .Where(eb => eb.State == BetState.Unmatched ||
                         eb.State == BetState.PartiallyMatched);

        // Filter by odds compatibility
        if (originalSide == BetSide.Back)
        {
            // Back bet: need Lay bets with odds <= our proposed odds
            query = query.Where(eb => eb.ProposedOdds <= proposedOdds);
        }
        else
        {
            // Lay bet: need Back bets with odds >= our proposed odds
            query = query.Where(eb => eb.ProposedOdds >= proposedOdds);
        }

        // Sort: best odds first, then earliest timestamp (FIFO)
        query = originalSide == BetSide.Back
            ? query.OrderBy(eb => eb.ProposedOdds).ThenBy(eb => eb.CreatedAt)
            : query.OrderByDescending(eb => eb.ProposedOdds).ThenBy(eb => eb.CreatedAt);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Get all unmatched bets for an outcome
    /// </summary>
    public async Task<List<ExchangeBet>> GetUnmatchedBetsAsync(
        Guid outcomeId,
        BetSide? side = null,
        int limit = 50)
    {
        var query = _context.ExchangeBets
            .Include(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
            .Where(eb => eb.Bet.Selections.Any(s => s.OutcomeId == outcomeId))
            .Where(eb => eb.State == BetState.Unmatched ||
                         eb.State == BetState.PartiallyMatched);

        if (side.HasValue)
        {
            query = query.Where(eb => eb.Side == side.Value);
        }

        return await query
            .OrderByDescending(eb => eb.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// Take (match) a specific unmatched bet
    /// Creates a counter-bet for the user
    /// </summary>
    public async Task<MatchResult> TakeBetAsync(
        Guid exchangeBetId,
        Guid userId,
        decimal stakeToMatch)
    {
        var targetBet = await _context.ExchangeBets
            .Include(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
            .FirstOrDefaultAsync(eb => eb.Id == exchangeBetId);

        if (targetBet == null)
            throw new ArgumentException("Exchange bet not found");

        if (targetBet.Bet.UserId == userId)
            throw new InvalidOperationException("Cannot match your own bet");

        if (stakeToMatch > targetBet.UnmatchedStake)
            throw new InvalidOperationException("Stake exceeds available unmatched amount");

        // Get user
        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new ArgumentException("User not found");

        // Get outcome, event, market
        var outcomeId = targetBet.Bet.Selections.First().OutcomeId;
        var outcome = await _context.Outcomes.FindAsync(outcomeId);

        if (outcome == null)
            throw new InvalidOperationException("Outcome not found");

        var market = await _context.Markets.FindAsync(outcome.MarketId);

        if (market == null)
            throw new InvalidOperationException("Market not found");

        var evt = await _context.Events.FindAsync(market.EventId);

        if (evt == null)
            throw new InvalidOperationException("Event not found");

        // Create opposite side
        var oppositeSide = targetBet.Side == BetSide.Back ? BetSide.Lay : BetSide.Back;

        // Create counter bet
        var counterBet = Bet.CreateExchangeSingle(
            user,
            new Money(stakeToMatch, user.Wallet!.Balance.Currency),
            evt,
            market,
            outcome,
            targetBet.ProposedOdds,
            oppositeSide);

        var counterExchangeBet = new ExchangeBet(
            counterBet,
            oppositeSide,
            targetBet.ProposedOdds,
            stakeToMatch);

        _context.Bets.Add(counterBet);
        _context.ExchangeBets.Add(counterExchangeBet);

        // Create match
        // targetBet is the maker (was in order book)
        // counterExchangeBet is the taker (just created to match)
        var match = new BetMatch(
            backBet: targetBet.Side == BetSide.Back ? targetBet : counterExchangeBet,
            layBet: targetBet.Side == BetSide.Lay ? targetBet : counterExchangeBet,
            matchedStake: stakeToMatch,
            matchedOdds: targetBet.ProposedOdds,
            makerBet: targetBet  // Target bet was in order book first
        );

        _context.BetMatches.Add(match);

        // Update states
        targetBet.ApplyMatch(stakeToMatch);
        counterExchangeBet.ApplyMatch(stakeToMatch);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} took bet {TargetBetId} for stake {Stake}",
            userId, exchangeBetId, stakeToMatch);

        // Send WebSocket notifications
        var matchResult = new MatchResult
        {
            FullyMatched = true,
            MatchedAmount = stakeToMatch,
            UnmatchedAmount = 0,
            Matches = new List<BetMatch> { match },
            Message = "Successfully matched bet"
        };

        // Notify both users
        await SendBetMatchedNotification(targetBet, new MatchResult
        {
            MatchedAmount = stakeToMatch,
            UnmatchedAmount = targetBet.UnmatchedStake,
            FullyMatched = targetBet.State == BetState.Matched,
            Matches = new List<BetMatch> { match }
        });

        await SendBetMatchedNotification(counterExchangeBet, matchResult);

        // Broadcast updated order book
        await BroadcastOrderBookUpdate(outcomeId);

        return matchResult;
    }

    /// <summary>
    /// Cancel an unmatched or partially matched bet
    /// </summary>
    public async Task CancelBetAsync(Guid exchangeBetId, Guid userId)
    {
        var bet = await _context.ExchangeBets
            .Include(eb => eb.Bet)
            .FirstOrDefaultAsync(eb => eb.Id == exchangeBetId);

        if (bet == null)
            throw new ArgumentException("Exchange bet not found");

        if (bet.Bet.UserId != userId)
            throw new UnauthorizedAccessException("Cannot cancel another user's bet");

        if (bet.State == BetState.Matched)
            throw new InvalidOperationException("Cannot cancel fully matched bet");

        if (bet.State == BetState.Cancelled)
            throw new InvalidOperationException("Bet is already cancelled");

        var outcomeId = bet.Bet.Selections.First().OutcomeId;
        var cancelledStake = bet.UnmatchedStake;

        bet.Cancel();
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} cancelled exchange bet {BetId}. Unmatched stake: {UnmatchedStake}",
            userId, exchangeBetId, cancelledStake);

        // Send WebSocket notification
        var message = new OrderCancelledMessage
        {
            OutcomeId = outcomeId,
            ExchangeBetId = exchangeBetId,
            Side = bet.Side,
            CancelledStake = cancelledStake
        };

        await _hubContext.Clients.Group($"outcome-{outcomeId}")
            .SendAsync("OrderCancelled", message);

        // Broadcast updated order book
        await BroadcastOrderBookUpdate(outcomeId);
    }

    /// <summary>
    /// Send bet matched notification to a user
    /// </summary>
    private async Task SendBetMatchedNotification(ExchangeBet bet, MatchResult result)
    {
        var userId = bet.Bet.UserId;
        var message = new BetMatchedMessage
        {
            BetId = bet.Bet.Id,
            ExchangeBetId = bet.Id,
            MatchedAmount = result.MatchedAmount,
            RemainingAmount = result.UnmatchedAmount,
            MatchedOdds = bet.ProposedOdds,
            FullyMatched = result.FullyMatched,
            Message = result.Message
        };

        await _hubContext.Clients.Group($"user-{userId}")
            .SendAsync("BetMatched", message);
    }

    /// <summary>
    /// Broadcast order book update to all watchers of an outcome
    /// </summary>
    private async Task BroadcastOrderBookUpdate(Guid outcomeId)
    {
        // Get current order book state
        var backOrders = await GetUnmatchedBetsAsync(outcomeId, BetSide.Back, 10);
        var layOrders = await GetUnmatchedBetsAsync(outcomeId, BetSide.Lay, 10);

        var message = new OrderBookUpdateMessage
        {
            OutcomeId = outcomeId,
            BackOrders = backOrders.Select(b => new OrderBookEntry
            {
                Odds = b.ProposedOdds,
                Stake = b.UnmatchedStake,
                CreatedAt = b.CreatedAt
            }).ToList(),
            LayOrders = layOrders.Select(b => new OrderBookEntry
            {
                Odds = b.ProposedOdds,
                Stake = b.UnmatchedStake,
                CreatedAt = b.CreatedAt
            }).ToList(),
            ConsensusBackOdds = backOrders.FirstOrDefault()?.ProposedOdds,
            ConsensusLayOdds = layOrders.FirstOrDefault()?.ProposedOdds
        };

        await _hubContext.Clients.Group($"outcome-{outcomeId}")
            .SendAsync("OrderBookUpdate", message);
    }
}
