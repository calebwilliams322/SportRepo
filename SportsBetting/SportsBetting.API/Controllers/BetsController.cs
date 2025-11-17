using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBetting.API.DTOs;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Exceptions;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;
using System.Security.Claims;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Require authentication for all endpoints in this controller
public class BetsController : ControllerBase
{
    private readonly SportsBettingDbContext _context;
    private readonly WalletService _walletService;
    private readonly SettlementService _settlementService;
    private readonly ILogger<BetsController> _logger;

    public BetsController(
        SportsBettingDbContext context,
        WalletService walletService,
        SettlementService settlementService,
        ILogger<BetsController> logger)
    {
        _context = context;
        _walletService = walletService;
        _settlementService = settlementService;
        _logger = logger;
    }

    /// <summary>
    /// Get bet by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType<BetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BetResponse>> GetBet(Guid id)
    {
        var bet = await _context.Bets
            .Include(b => b.Selections)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bet == null)
        {
            return NotFound(new { message = $"Bet {id} not found" });
        }

        // AUTHORIZATION: Check if user can access this bet
        if (!CanAccessBet(bet.UserId))
        {
            _logger.LogWarning("User {CurrentUser} attempted to access bet {BetId} owned by {BetOwner}",
                GetCurrentUserId(), id, bet.UserId);
            return Forbid();
        }

        return Ok(MapToBetResponse(bet));
    }

    /// <summary>
    /// Get bets for a user
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType<List<BetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<BetResponse>>> GetUserBets(
        Guid userId,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // AUTHORIZATION: Check if user can access these bets
        if (!CanAccessBet(userId))
        {
            _logger.LogWarning("User {CurrentUser} attempted to access bets for user {RequestedUser}",
                GetCurrentUserId(), userId);
            return Forbid();
        }

        var query = _context.Bets
            .Include(b => b.Selections)
            .Where(b => b.UserId == userId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BetStatus>(status, true, out var betStatus))
        {
            // Filter by status if provided
            query = query.Where(b => b.Status == betStatus);
        }

        var bets = await query
            .OrderByDescending(b => b.PlacedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(bets.Select(MapToBetResponse).ToList());
    }

    /// <summary>
    /// Place a single bet (for the currently logged-in user)
    /// </summary>
    [HttpPost("single")]
    [ProducesResponseType<BetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BetResponse>> PlaceSingleBet(
        [FromBody] PlaceBetRequest request)
    {
        // AUTHORIZATION: Get current user ID from JWT token
        // Users can ONLY place bets for themselves
        var userId = GetCurrentUserId();

        // SECURITY: Prevent Support users from placing bets (conflict of interest)
        if (User.IsInRole("Support"))
        {
            _logger.LogWarning("Support user {UserId} attempted to place a bet (forbidden)", userId);
            return Forbid();
        }

        if (request.Stake <= 0)
        {
            return BadRequest(new { message = "Stake must be greater than zero" });
        }

        // Retry logic for optimistic concurrency on wallet updates
        const int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                // Reload user with wallet on each retry to get latest RowVersion
                var user = await _context.Users
                    .Include(u => u.Wallet)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new { message = $"User {userId} not found" });
                }

                // Load event with markets and outcomes (these don't need retry)
                var evt = await _context.Events
                    .Include(e => e.Markets)
                        .ThenInclude(m => m.Outcomes)
                    .FirstOrDefaultAsync(e => e.Id == request.EventId);

                if (evt == null)
                {
                    return NotFound(new { message = $"Event {request.EventId} not found" });
                }

                var market = evt.Markets.FirstOrDefault(m => m.Id == request.MarketId);
                if (market == null)
                {
                    return NotFound(new { message = $"Market {request.MarketId} not found" });
                }

                var outcome = market.Outcomes.FirstOrDefault(o => o.Id == request.OutcomeId);
                if (outcome == null)
                {
                    return NotFound(new { message = $"Outcome {request.OutcomeId} not found" });
                }

                // Validate market is open
                if (!market.IsOpen)
                {
                    return BadRequest(new { message = "Market is closed" });
                }

                // Create bet
                var bet = Bet.CreateSingle(
                    user,
                    new Money(request.Stake, user.Wallet!.Balance.Currency),
                    evt,
                    market,
                    outcome
                );

                // Process wallet transaction
                var transaction = _walletService.PlaceBet(user, bet);

                // Save to database
                _context.Bets.Add(bet);
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} placed bet {BetId} for {Stake} (attempt {Attempt})",
                    userId, bet.Id, request.Stake, retryCount + 1);

                return CreatedAtAction(
                    nameof(GetBet),
                    new { id = bet.Id },
                    MapToBetResponse(bet)
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                _logger.LogWarning("Concurrency conflict placing bet for user {UserId}, retry {Retry}/{MaxRetries}",
                    userId, retryCount, maxRetries);

                // Clear the context to avoid tracking conflicts
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, "Max retries exceeded placing bet for user {UserId}", userId);
                    return Conflict(new { message = "Unable to place bet due to concurrent updates. Please try again." });
                }

                // Small delay before retry to reduce contention
                await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        return Conflict(new { message = "Unable to place bet. Please try again." });
    }

    /// <summary>
    /// Place a parlay bet (for the currently logged-in user)
    /// </summary>
    [HttpPost("parlay")]
    [ProducesResponseType<BetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BetResponse>> PlaceParlayBet(
        [FromBody] PlaceParlayBetRequest request)
    {
        // AUTHORIZATION: Get current user ID from JWT token
        // Users can ONLY place bets for themselves
        var userId = GetCurrentUserId();

        // SECURITY: Prevent Support users from placing bets (conflict of interest)
        if (User.IsInRole("Support"))
        {
            _logger.LogWarning("Support user {UserId} attempted to place a parlay bet (forbidden)", userId);
            return Forbid();
        }

        if (request.Stake <= 0)
        {
            return BadRequest(new { message = "Stake must be greater than zero" });
        }

        if (request.Legs.Count < 2)
        {
            return BadRequest(new { message = "Parlay must have at least 2 legs" });
        }

        // Retry logic for optimistic concurrency on wallet updates
        const int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                // Reload user with wallet on each retry to get latest RowVersion
                var user = await _context.Users
                    .Include(u => u.Wallet)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new { message = $"User {userId} not found" });
                }

                // Build list of (Event, Market, Outcome) tuples for the parlay
                var selections = new List<(Event, Market, Outcome)>();

                foreach (var leg in request.Legs)
                {
                    var evt = await _context.Events
                        .Include(e => e.Markets)
                            .ThenInclude(m => m.Outcomes)
                        .FirstOrDefaultAsync(e => e.Id == leg.EventId);

                    if (evt == null)
                    {
                        return NotFound(new { message = $"Event {leg.EventId} not found" });
                    }

                    var market = evt.Markets.FirstOrDefault(m => m.Id == leg.MarketId);
                    if (market == null)
                    {
                        return NotFound(new { message = $"Market {leg.MarketId} not found in event {leg.EventId}" });
                    }

                    if (!market.IsOpen)
                    {
                        return BadRequest(new { message = $"Market {market.Name} is closed" });
                    }

                    var outcome = market.Outcomes.FirstOrDefault(o => o.Id == leg.OutcomeId);
                    if (outcome == null)
                    {
                        return NotFound(new { message = $"Outcome {leg.OutcomeId} not found in market {leg.MarketId}" });
                    }

                    selections.Add((evt, market, outcome));
                }

                // Create parlay bet
                var bet = Bet.CreateParlay(
                    user,
                    new Money(request.Stake, user.Wallet!.Balance.Currency),
                    selections.ToArray()
                );

                // Process wallet transaction
                var transaction = _walletService.PlaceBet(user, bet);

                // Save to database
                _context.Bets.Add(bet);
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} placed parlay bet {BetId} with {LegCount} legs for {Stake} (attempt {Attempt})",
                    userId, bet.Id, request.Legs.Count, request.Stake, retryCount + 1);

                return CreatedAtAction(
                    nameof(GetBet),
                    new { id = bet.Id },
                    MapToBetResponse(bet)
                );
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                _logger.LogWarning("Concurrency conflict placing parlay bet for user {UserId}, retry {Retry}/{MaxRetries}",
                    userId, retryCount, maxRetries);

                // Clear the context to avoid tracking conflicts
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, "Max retries exceeded placing parlay bet for user {UserId}", userId);
                    return Conflict(new { message = "Unable to place bet due to concurrent updates. Please try again." });
                }

                // Small delay before retry to reduce contention
                await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        return Conflict(new { message = "Unable to place parlay bet. Please try again." });
    }

    /// <summary>
    /// Settle a bet and process payout if won (Admin only)
    /// </summary>
    [HttpPost("{id}/settle")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType<BetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BetResponse>> SettleBet(Guid id)
    {
        var bet = await _context.Bets
            .Include(b => b.Selections)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bet == null)
        {
            return NotFound(new { message = $"Bet {id} not found" });
        }

        // Load all events for the bet's selections
        var eventIds = bet.Selections.Select(s => s.EventId).Distinct();
        var events = await _context.Events
            .Include(e => e.Markets)
                .ThenInclude(m => m.Outcomes)
            .Where(e => eventIds.Contains(e.Id))
            .ToListAsync();

        // Load user for payout
        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == bet.UserId);

        if (user == null)
        {
            return NotFound(new { message = $"User {bet.UserId} not found" });
        }

        try
        {
            // Settle the bet
            _settlementService.SettleBet(bet, events);

            // Process payout if bet won
            if (bet.Status == BetStatus.Won)
            {
                var transaction = _walletService.SettleBet(user, bet);
                if (transaction != null)
                {
                    _context.Transactions.Add(transaction);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Settled bet {BetId} with status {Status}", bet.Id, bet.Status);

            return Ok(MapToBetResponse(bet));
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

    private BetResponse MapToBetResponse(Bet bet)
    {
        return new BetResponse(
            bet.Id,
            bet.UserId,
            bet.TicketNumber,
            bet.Type.ToString(),
            bet.Status.ToString(),
            bet.Stake.Amount,
            bet.Stake.Currency,
            bet.CombinedOdds.DecimalValue,
            bet.PotentialPayout.Amount,
            bet.ActualPayout?.Amount,
            bet.PlacedAt,
            bet.SettledAt,
            bet.Selections.Select(s => new BetSelectionResponse(
                s.Id,
                s.EventId,
                s.EventName,
                s.MarketId,
                s.MarketType.ToString(),
                s.MarketName,
                s.OutcomeId,
                s.OutcomeName,
                s.LockedOdds.DecimalValue,
                s.Line,
                s.Result.ToString()
            )).ToList()
        );
    }

    // ============ AUTHORIZATION HELPER METHODS ============

    /// <summary>
    /// Get the current logged-in user's ID from JWT claims
    /// </summary>
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user ID in token");
        }
        return userId;
    }

    /// <summary>
    /// Check if the current user is an admin
    /// </summary>
    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }

    /// <summary>
    /// Check if the current user can access a bet
    /// Admins and Support can access any bet, regular users can only access their own
    /// </summary>
    private bool CanAccessBet(Guid betUserId)
    {
        if (User.IsInRole("Support") || User.IsInRole("Admin"))
            return true;

        var currentUserId = GetCurrentUserId();
        return currentUserId == betUserId;
    }
}
