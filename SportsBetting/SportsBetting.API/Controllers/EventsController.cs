using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBetting.API.DTOs;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Exceptions;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly SportsBettingDbContext _context;
    private readonly SettlementService _settlementService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        SportsBettingDbContext context,
        SettlementService settlementService,
        ILogger<EventsController> logger)
    {
        _context = context;
        _settlementService = settlementService;
        _logger = logger;
    }

    /// <summary>
    /// Get all events
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<EventResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EventResponse>>> GetEvents(
        [FromQuery] string? status = null,
        [FromQuery] Guid? leagueId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.Events
            .Include(e => e.HomeTeam)
            .Include(e => e.AwayTeam)
            .Include(e => e.Markets)
                .ThenInclude(m => m.Outcomes)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<EventStatus>(status, true, out var eventStatus))
        {
            query = query.Where(e => e.Status == eventStatus);
        }

        if (leagueId.HasValue)
        {
            query = query.Where(e => e.LeagueId == leagueId.Value);
        }

        var events = await query
            .OrderBy(e => e.ScheduledStartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = events.Select(e => MapToEventResponse(e)).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEvent(Guid id)
    {
        var evt = await _context.Events
            .Include(e => e.HomeTeam)
            .Include(e => e.AwayTeam)
            .Include(e => e.Markets)
                .ThenInclude(m => m.Outcomes)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null)
        {
            return NotFound(new { message = $"Event {id} not found" });
        }

        return Ok(MapToEventResponse(evt));
    }

    /// <summary>
    /// Start an event
    /// </summary>
    [HttpPost("{id}/start")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> StartEvent(Guid id)
    {
        var evt = await _context.Events
            .Include(e => e.HomeTeam)
            .Include(e => e.AwayTeam)
            .Include(e => e.Markets)
                .ThenInclude(m => m.Outcomes)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null)
        {
            return NotFound(new { message = $"Event {id} not found" });
        }

        try
        {
            evt.Start();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Started event {EventId} - {EventName}", evt.Id, evt.Name);

            return Ok(MapToEventResponse(evt));
        }
        catch (BettingException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Complete an event with final score
    /// </summary>
    [HttpPost("{id}/complete")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> CompleteEvent(Guid id, [FromBody] CompleteEventRequest request)
    {
        var evt = await _context.Events
            .Include(e => e.HomeTeam)
            .Include(e => e.AwayTeam)
            .Include(e => e.Markets)
                .ThenInclude(m => m.Outcomes)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null)
        {
            return NotFound(new { message = $"Event {id} not found" });
        }

        try
        {
            evt.Complete(new Score(request.HomeScore, request.AwayScore));
            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed event {EventId} - {EventName} with score {Score}",
                evt.Id, evt.Name, $"{request.HomeScore}-{request.AwayScore}");

            return Ok(MapToEventResponse(evt));
        }
        catch (BettingException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Settle all markets for an event based on final score
    /// </summary>
    [HttpPost("{id}/settle")]
    [ProducesResponseType<EventResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> SettleEvent(Guid id)
    {
        var evt = await _context.Events
            .Include(e => e.HomeTeam)
            .Include(e => e.AwayTeam)
            .Include(e => e.Markets)
                .ThenInclude(m => m.Outcomes)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null)
        {
            return NotFound(new { message = $"Event {id} not found" });
        }

        try
        {
            _settlementService.SettleEvent(evt);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Settled all markets for event {EventId} - {EventName}", evt.Id, evt.Name);

            return Ok(MapToEventResponse(evt));
        }
        catch (BettingException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get market by ID with all outcomes
    /// </summary>
    [HttpGet("markets/{marketId}")]
    [ProducesResponseType<MarketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketResponse>> GetMarket(Guid marketId)
    {
        var market = await _context.Markets
            .Include(m => m.Outcomes)
            .FirstOrDefaultAsync(m => m.Id == marketId);

        if (market == null)
        {
            return NotFound(new { message = $"Market {marketId} not found" });
        }

        return Ok(MapToMarketResponse(market));
    }

    /// <summary>
    /// Update odds for an outcome
    /// </summary>
    [HttpPut("outcomes/{outcomeId}/odds")]
    [ProducesResponseType<OutcomeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OutcomeResponse>> UpdateOdds(Guid outcomeId, [FromBody] UpdateOddsRequest request)
    {
        if (request.NewOdds < 1.0m)
        {
            return BadRequest(new { message = "Odds must be at least 1.0" });
        }

        var outcome = await _context.Outcomes.FindAsync(outcomeId);

        if (outcome == null)
        {
            return NotFound(new { message = $"Outcome {outcomeId} not found" });
        }

        outcome.UpdateOdds(new Odds(request.NewOdds));
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated odds for outcome {OutcomeId} to {NewOdds}", outcomeId, request.NewOdds);

        return Ok(new OutcomeResponse(
            outcome.Id,
            outcome.MarketId,
            outcome.Name,
            outcome.Description,
            outcome.CurrentOdds.DecimalValue,
            outcome.Line,
            outcome.IsWinner,
            outcome.IsVoid
        ));
    }

    private EventResponse MapToEventResponse(Event evt)
    {
        // Teams are not navigation properties, they're stored inline
        // So we need to get the IDs from shadow properties
        return new EventResponse(
            evt.Id,
            evt.Name,
            evt.LeagueId,
            evt.HomeTeam.Id,
            evt.HomeTeam.Name,
            evt.AwayTeam.Id,
            evt.AwayTeam.Name,
            evt.ScheduledStartTime,
            evt.Venue,
            evt.Status.ToString(),
            evt.FinalScore.HasValue ? $"{evt.FinalScore.Value.HomeScore}-{evt.FinalScore.Value.AwayScore}" : null,
            evt.Markets.Select(m => new MarketSummary(
                m.Id,
                m.Type.ToString(),
                m.Name,
                m.IsOpen,
                m.Outcomes.Count
            )).ToList()
        );
    }

    private MarketResponse MapToMarketResponse(Market market)
    {
        return new MarketResponse(
            market.Id,
            market.EventId,
            market.Type.ToString(),
            market.Name,
            market.Description,
            market.IsOpen,
            market.IsSettled,
            market.Outcomes.Select(o => new OutcomeResponse(
                o.Id,
                o.MarketId,
                o.Name,
                o.Description,
                o.CurrentOdds.DecimalValue,
                o.Line,
                o.IsWinner,
                o.IsVoid
            )).ToList()
        );
    }
}
