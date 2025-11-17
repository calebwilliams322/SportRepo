using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBetting.API.Services;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.API.Controllers;

/// <summary>
/// Admin endpoints for settling events and bets
/// Automatically tracks revenue for both sportsbook and exchange bets
/// </summary>
[ApiController]
[Route("api/admin/settlement")]
[Authorize(Roles = "Admin")]
public class SettlementController : ControllerBase
{
    private readonly SportsBettingDbContext _context;
    private readonly SettlementService _settlementService;
    private readonly IRevenueService _revenueService;
    private readonly ILogger<SettlementController> _logger;

    public SettlementController(
        SportsBettingDbContext context,
        SettlementService settlementService,
        IRevenueService revenueService,
        ILogger<SettlementController> logger)
    {
        _context = context;
        _settlementService = settlementService;
        _revenueService = revenueService;
        _logger = logger;
    }

    /// <summary>
    /// Settle an event by providing the final score
    /// Settles all markets and bets for the event, and tracks revenue
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="request">Final score</param>
    [HttpPost("event/{eventId}")]
    public async Task<ActionResult<SettlementResult>> SettleEvent(
        Guid eventId,
        [FromBody] SettleEventRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Load event with all related data
            var evt = await _context.Events
                .Include(e => e.Markets)
                    .ThenInclude(m => m.Outcomes)
                .Include(e => e.HomeTeam)
                .Include(e => e.AwayTeam)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (evt == null)
                return NotFound($"Event {eventId} not found");

            if (evt.Status == EventStatus.Completed)
                return BadRequest("Event is already completed");

            // Set final score and complete the event
            var finalScore = new Score(request.HomeScore, request.AwayScore);
            evt.Complete(finalScore);

            // Settle all markets for the event
            _settlementService.SettleEvent(evt);

            var result = new SettlementResult
            {
                EventId = eventId,
                EventName = $"{evt.HomeTeam.Name} vs {evt.AwayTeam.Name}",
                FinalScore = $"{request.HomeScore}-{request.AwayScore}",
                MarketsSettled = evt.Markets.Count(m => m.IsSettled)
            };

            await _context.SaveChangesAsync();

            // Now settle all bets for this event
            await SettleBetsForEvent(evt, result);

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Event {EventId} settled: {BetsSettled} bets, ${Revenue} revenue tracked",
                eventId, result.BetsSettled, result.TotalRevenueRecorded);

            return Ok(result);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error settling event {EventId}", eventId);
            return StatusCode(500, $"Error settling event: {ex.Message}");
        }
    }

    /// <summary>
    /// Settle all sportsbook and exchange bets for an event
    /// Automatically tracks revenue
    /// </summary>
    private async Task SettleBetsForEvent(Event evt, SettlementResult result)
    {
        // 1. Settle sportsbook bets
        var sportsbookBets = await _context.Bets
            .Include(b => b.Selections)
            .Where(b => b.Selections.Any(s => s.EventId == evt.Id) &&
                       b.BetMode == BetMode.Sportsbook &&
                       b.Status == BetStatus.Pending)
            .ToListAsync();

        foreach (var bet in sportsbookBets)
        {
            try
            {
                _settlementService.SettleBet(bet, new[] { evt });

                // Track revenue for this sportsbook bet
                _revenueService.RecordSportsbookSettlement(bet);

                result.BetsSettled++;
                if (bet.Status == BetStatus.Won)
                    result.SportsbookWinningBets++;
                else if (bet.Status == BetStatus.Lost)
                    result.SportsbookLosingBets++;

                _logger.LogDebug(
                    "Settled sportsbook bet {BetId}, status: {Status}",
                    bet.Id, bet.Status);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to settle bet {BetId}", bet.Id);
                result.FailedBets++;
            }
        }

        await _context.SaveChangesAsync();

        // 2. Settle exchange bets
        var exchangeMatches = await _context.BetMatches
            .Include(m => m.BackBet)
                .ThenInclude(eb => eb.Bet)
                    .ThenInclude(b => b.User)
                        .ThenInclude(u => u.Statistics)
            .Include(m => m.BackBet)
                .ThenInclude(eb => eb.Bet)
                    .ThenInclude(b => b.Selections)
            .Include(m => m.LayBet)
                .ThenInclude(eb => eb.Bet)
                    .ThenInclude(b => b.User)
                        .ThenInclude(u => u.Statistics)
            .Include(m => m.LayBet)
                .ThenInclude(eb => eb.Bet)
                    .ThenInclude(b => b.Selections)
            .Where(m => !m.IsSettled &&
                       (m.BackBet.Bet.Selections.Any(s => s.EventId == evt.Id) ||
                        m.LayBet.Bet.Selections.Any(s => s.EventId == evt.Id)))
            .ToListAsync();

        foreach (var match in exchangeMatches)
        {
            try
            {
                // Find the outcome for this match
                var selection = match.BackBet.Bet.Selections.FirstOrDefault(s => s.EventId == evt.Id);
                if (selection == null) continue;

                var market = evt.Markets.FirstOrDefault(m => m.Id == selection.MarketId);
                if (market == null || !market.IsSettled) continue;

                var outcome = market.Outcomes.FirstOrDefault(o => o.Id == selection.OutcomeId);
                if (outcome == null || !outcome.IsWinner.HasValue) continue;

                var backUser = match.BackBet.Bet.User;
                var layUser = match.LayBet.Bet.User;

                // Settle the exchange match
                var (winnerBet, netWinnings) = _settlementService.SettleExchangeMatch(
                    match, outcome, backUser, layUser
                );

                // Track revenue for this exchange match
                var backBetWon = match.WinnerBetId == match.BackBetId;
                var commission = backBetWon
                    ? match.BackBetCommission ?? 0m
                    : match.LayBetCommission ?? 0m;
                var winnerPayout = match.MatchedStake + netWinnings.Amount;

                _revenueService.RecordExchangeSettlement(
                    match, commission, winnerPayout
                );

                result.BetsSettled += 2; // Count both back and lay bets
                result.ExchangeMatchesSettled++;
                result.ExchangeCommissionEarned += commission;

                _logger.LogDebug(
                    "Settled exchange match {MatchId}, commission: ${Commission}",
                    match.Id, commission);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to settle exchange match {MatchId}", match.Id);
                result.FailedBets++;
            }
        }

        await _context.SaveChangesAsync();

        // Get total revenue recorded
        var revenue = await _context.HouseRevenue
            .Where(r => r.PeriodStart <= DateTime.UtcNow && r.PeriodEnd > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        result.TotalRevenueRecorded = revenue?.TotalRevenue ?? 0m;
    }

    /// <summary>
    /// Get all unsettled events
    /// </summary>
    [HttpGet("unsettled-events")]
    public async Task<ActionResult<List<UnsettledEventDto>>> GetUnsettledEvents()
    {
        var events = await _context.Events
            .Include(e => e.HomeTeam)
            .Include(e => e.AwayTeam)
            .Include(e => e.Markets)
            .Where(e => e.Status != EventStatus.Completed && e.Status != EventStatus.Cancelled)
            .OrderBy(e => e.ScheduledStartTime)
            .Select(e => new UnsettledEventDto
            {
                EventId = e.Id,
                HomeTeam = e.HomeTeam.Name,
                AwayTeam = e.AwayTeam.Name,
                StartTime = e.ScheduledStartTime,
                Status = e.Status.ToString(),
                MarketsCount = e.Markets.Count,
                HasFinalScore = e.FinalScore != null
            })
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>
    /// Get pending bets for an event
    /// </summary>
    [HttpGet("event/{eventId}/pending-bets")]
    public async Task<ActionResult<PendingBetsDto>> GetPendingBets(Guid eventId)
    {
        var sportsbookCount = await _context.Bets
            .Where(b => b.Selections.Any(s => s.EventId == eventId) &&
                       b.BetMode == BetMode.Sportsbook &&
                       b.Status == BetStatus.Pending)
            .CountAsync();

        var exchangeCount = await _context.BetMatches
            .Where(m => !m.IsSettled &&
                       (m.BackBet.Bet.Selections.Any(s => s.EventId == eventId) ||
                        m.LayBet.Bet.Selections.Any(s => s.EventId == eventId)))
            .CountAsync();

        return Ok(new PendingBetsDto
        {
            EventId = eventId,
            SportsbookBetsCount = sportsbookCount,
            ExchangeMatchesCount = exchangeCount,
            TotalPendingBets = sportsbookCount + (exchangeCount * 2)
        });
    }
}

/// <summary>
/// Request to settle an event with final score
/// </summary>
public class SettleEventRequest
{
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
}

/// <summary>
/// Result of settling an event
/// </summary>
public class SettlementResult
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = "";
    public string FinalScore { get; set; } = "";
    public int MarketsSettled { get; set; }
    public int BetsSettled { get; set; }
    public int SportsbookWinningBets { get; set; }
    public int SportsbookLosingBets { get; set; }
    public int ExchangeMatchesSettled { get; set; }
    public decimal ExchangeCommissionEarned { get; set; }
    public int FailedBets { get; set; }
    public decimal TotalRevenueRecorded { get; set; }
}

/// <summary>
/// Unsettled event info
/// </summary>
public class UnsettledEventDto
{
    public Guid EventId { get; set; }
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public DateTime StartTime { get; set; }
    public string Status { get; set; } = "";
    public int MarketsCount { get; set; }
    public bool HasFinalScore { get; set; }
}

/// <summary>
/// Pending bets for an event
/// </summary>
public class PendingBetsDto
{
    public Guid EventId { get; set; }
    public int SportsbookBetsCount { get; set; }
    public int ExchangeMatchesCount { get; set; }
    public int TotalPendingBets { get; set; }
}
