using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBetting.API.DTOs;
using SportsBetting.API.Services;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;
using System.Security.Claims;

namespace SportsBetting.API.Controllers;

/// <summary>
/// Controller for P2P exchange betting functionality
/// Provides endpoints for placing, matching, and managing exchange bets
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExchangeController : ControllerBase
{
    private readonly SportsBettingDbContext _context;
    private readonly IBetMatchingService _matchingService;
    private readonly IOddsValidationService _oddsValidationService;
    private readonly ILogger<ExchangeController> _logger;

    public ExchangeController(
        SportsBettingDbContext context,
        IBetMatchingService matchingService,
        IOddsValidationService oddsValidationService,
        ILogger<ExchangeController> logger)
    {
        _context = context;
        _matchingService = matchingService;
        _oddsValidationService = oddsValidationService;
        _logger = logger;
    }

    /// <summary>
    /// Place a new exchange bet (Back or Lay)
    /// </summary>
    /// <remarks>
    /// Places a P2P exchange bet and attempts to match it with existing opposing bets.
    /// Uses FIFO matching algorithm: best odds first, then earliest timestamp.
    ///
    /// - Back bet: Betting FOR an outcome to occur (traditional bet)
    /// - Lay bet: Betting AGAINST an outcome (acting as the bookmaker)
    ///
    /// Validates proposed odds against market consensus (20% tolerance with warnings).
    /// </remarks>
    [HttpPost("bets")]
    [ProducesResponseType(typeof(PlaceExchangeBetResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> PlaceExchangeBet([FromBody] PlaceExchangeBetRequest request)
    {
        try
        {
            // Get user ID from JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user ID in token" });
            }

            // Validation
            if (request.ProposedOdds < 1.0m)
            {
                return BadRequest(new { message = "Proposed odds must be at least 1.0" });
            }

            if (request.Stake <= 0)
            {
                return BadRequest(new { message = "Stake must be greater than 0" });
            }

            // Get user with wallet
            var user = await _context.Users
                .Include(u => u.Wallet)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (user.Wallet == null)
            {
                return BadRequest(new { message = "User does not have a wallet" });
            }

            // Get outcome, market, and event
            var outcome = await _context.Outcomes.FindAsync(request.OutcomeId);
            if (outcome == null)
            {
                return NotFound(new { message = "Outcome not found" });
            }

            var market = await _context.Markets.FindAsync(outcome.MarketId);
            if (market == null)
            {
                return NotFound(new { message = "Market not found" });
            }

            if (market.Mode == MarketMode.Sportsbook)
            {
                return BadRequest(new { message = "Cannot place exchange bets on sportsbook-only markets" });
            }

            var evt = await _context.Events.FindAsync(market.EventId);
            if (evt == null)
            {
                return NotFound(new { message = "Event not found" });
            }

            // Validate odds against consensus
            var oddsValidation = await _oddsValidationService.ValidateOddsAsync(
                request.OutcomeId,
                request.ProposedOdds,
                tolerancePercent: 20.0m);

            // Create the bet
            var bet = Bet.CreateExchangeSingle(
                user,
                new Money(request.Stake, user.Wallet.Balance.Currency),
                evt,
                market,
                outcome,
                request.ProposedOdds,
                request.Side);

            var exchangeBet = new ExchangeBet(
                bet,
                request.Side,
                request.ProposedOdds,
                request.Stake);

            _context.Bets.Add(bet);
            _context.ExchangeBets.Add(exchangeBet);
            await _context.SaveChangesAsync();

            // Attempt to match the bet
            var matchResult = await _matchingService.MatchBetAsync(exchangeBet);

            _logger.LogInformation(
                "User {UserId} placed {Side} bet on outcome {OutcomeId} at odds {Odds} for stake {Stake}. " +
                "Matched: {MatchedAmount}, Unmatched: {UnmatchedAmount}",
                userId, request.Side, request.OutcomeId, request.ProposedOdds, request.Stake,
                matchResult.MatchedAmount, matchResult.UnmatchedAmount);

            return Ok(new PlaceExchangeBetResponse
            {
                ExchangeBetId = exchangeBet.Id,
                FullyMatched = matchResult.FullyMatched,
                MatchedAmount = matchResult.MatchedAmount,
                UnmatchedAmount = matchResult.UnmatchedAmount,
                MatchCount = matchResult.Matches.Count,
                Message = matchResult.Message,
                OddsValidation = new OddsValidationDto
                {
                    IsValid = oddsValidation.IsValid,
                    ConsensusOdds = oddsValidation.ConsensusOdds,
                    ProposedOdds = oddsValidation.ProposedOdds,
                    DeviationPercent = oddsValidation.DeviationPercent,
                    HasWarning = oddsValidation.HasWarning,
                    Message = oddsValidation.Message
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing exchange bet");
            return StatusCode(500, new { message = "An error occurred while placing the bet" });
        }
    }

    /// <summary>
    /// Get unmatched bets for an outcome
    /// </summary>
    /// <remarks>
    /// Retrieves all unmatched or partially matched bets for a specific outcome.
    /// Useful for displaying the exchange order book to users.
    ///
    /// Results are ordered by creation time (most recent first).
    /// </remarks>
    /// <param name="outcomeId">The outcome to get bets for</param>
    /// <param name="side">Optional: Filter by side (Back or Lay)</param>
    /// <param name="limit">Maximum number of bets to return (default: 50)</param>
    [HttpGet("bets/unmatched")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ExchangeBetDto>), 200)]
    public async Task<IActionResult> GetUnmatchedBets(
        [FromQuery] Guid outcomeId,
        [FromQuery] BetSide? side = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            if (limit < 1 || limit > 100)
            {
                limit = 50;
            }

            var bets = await _matchingService.GetUnmatchedBetsAsync(outcomeId, side, limit);

            var dtos = bets.Select(eb => new ExchangeBetDto
            {
                Id = eb.Id,
                BetId = eb.BetId,
                UserId = eb.Bet.UserId,
                Username = eb.Bet.User?.Username ?? "Unknown",
                Side = eb.Side,
                SideName = eb.Side.ToString(),
                ProposedOdds = eb.ProposedOdds,
                TotalStake = eb.TotalStake,
                MatchedStake = eb.MatchedStake,
                UnmatchedStake = eb.UnmatchedStake,
                State = eb.State,
                StateName = eb.State.ToString(),
                CreatedAt = eb.CreatedAt
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unmatched bets for outcome {OutcomeId}", outcomeId);
            return StatusCode(500, new { message = "An error occurred while retrieving unmatched bets" });
        }
    }

    /// <summary>
    /// Take (match) a specific unmatched bet
    /// </summary>
    /// <remarks>
    /// Manually take an existing exchange bet by creating a counter-bet.
    /// Creates an immediate match between your new bet and the target bet.
    ///
    /// - Taking a Back bet: You place a Lay bet
    /// - Taking a Lay bet: You place a Back bet
    ///
    /// You cannot take your own bets.
    /// </remarks>
    /// <param name="exchangeBetId">The ID of the exchange bet to take</param>
    /// <param name="request">The amount to match</param>
    [HttpPost("bets/{exchangeBetId}/take")]
    [ProducesResponseType(typeof(TakeBetResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> TakeBet(
        Guid exchangeBetId,
        [FromBody] TakeBetRequest request)
    {
        try
        {
            // Get user ID from JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user ID in token" });
            }

            // Validation
            if (request.StakeToMatch <= 0)
            {
                return BadRequest(new { message = "Stake to match must be greater than 0" });
            }

            var matchResult = await _matchingService.TakeBetAsync(exchangeBetId, userId, request.StakeToMatch);

            if (matchResult.Matches.Count == 0)
            {
                return BadRequest(new { message = "Failed to create match" });
            }

            var match = matchResult.Matches.First();
            var yourBetId = match.BackBet.Side == BetSide.Back && match.BackBet.Bet.UserId == userId
                ? match.BackBet.BetId
                : match.LayBet.BetId;

            _logger.LogInformation(
                "User {UserId} took bet {ExchangeBetId} for stake {Stake}",
                userId, exchangeBetId, request.StakeToMatch);

            return Ok(new TakeBetResponse
            {
                MatchId = match.Id,
                YourBetId = yourBetId,
                MatchedAmount = match.MatchedStake,
                MatchedOdds = match.MatchedOdds,
                Message = matchResult.Message
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error taking bet {ExchangeBetId}", exchangeBetId);
            return StatusCode(500, new { message = "An error occurred while taking the bet" });
        }
    }

    /// <summary>
    /// Cancel an unmatched or partially matched bet
    /// </summary>
    /// <remarks>
    /// Cancels the unmatched portion of a bet.
    /// Matched portions cannot be cancelled.
    /// You can only cancel your own bets.
    /// </remarks>
    /// <param name="exchangeBetId">The ID of the exchange bet to cancel</param>
    [HttpDelete("bets/{exchangeBetId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CancelBet(Guid exchangeBetId)
    {
        try
        {
            // Get user ID from JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user ID in token" });
            }

            await _matchingService.CancelBetAsync(exchangeBetId, userId);

            _logger.LogInformation(
                "User {UserId} cancelled exchange bet {ExchangeBetId}",
                userId, exchangeBetId);

            return Ok(new { message = "Bet cancelled successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling bet {ExchangeBetId}", exchangeBetId);
            return StatusCode(500, new { message = "An error occurred while cancelling the bet" });
        }
    }

    /// <summary>
    /// Get consensus odds for an outcome
    /// </summary>
    /// <remarks>
    /// Retrieves the current market consensus odds from external sources (e.g., The Odds API).
    /// Consensus odds are used to validate user-proposed odds and detect outliers.
    ///
    /// Returns null if no consensus data is available or if it has expired.
    /// </remarks>
    /// <param name="outcomeId">The outcome to get consensus odds for</param>
    [HttpGet("odds/consensus/{outcomeId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ConsensusOddsDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetConsensusOdds(Guid outcomeId)
    {
        try
        {
            var consensus = await _oddsValidationService.GetConsensusOddsAsync(outcomeId);

            if (consensus == null)
            {
                return NotFound(new { message = "No consensus odds available for this outcome" });
            }

            var outcome = await _context.Outcomes.FindAsync(outcomeId);

            var dto = new ConsensusOddsDto
            {
                OutcomeId = consensus.OutcomeId,
                OutcomeName = outcome?.Name ?? "Unknown",
                AverageOdds = consensus.AverageOdds,
                MinOdds = consensus.MinOdds,
                MaxOdds = consensus.MaxOdds,
                SampleSize = consensus.SampleSize,
                Source = consensus.Source,
                FetchedAt = consensus.FetchedAt,
                ExpiresAt = consensus.ExpiresAt,
                IsExpired = consensus.IsExpired()
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consensus odds for outcome {OutcomeId}", outcomeId);
            return StatusCode(500, new { message = "An error occurred while retrieving consensus odds" });
        }
    }

    /// <summary>
    /// Get my exchange bets
    /// </summary>
    /// <remarks>
    /// Retrieves all exchange bets for the currently authenticated user.
    /// Includes matched, partially matched, unmatched, and cancelled bets.
    /// </remarks>
    /// <param name="state">Optional: Filter by state</param>
    /// <param name="limit">Maximum number of bets to return (default: 50)</param>
    [HttpGet("bets/my")]
    [ProducesResponseType(typeof(List<ExchangeBetDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetMyBets(
        [FromQuery] BetState? state = null,
        [FromQuery] int limit = 50)
    {
        try
        {
            // Get user ID from JWT
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid or missing user ID in token" });
            }

            if (limit < 1 || limit > 100)
            {
                limit = 50;
            }

            var query = _context.ExchangeBets
                .Include(eb => eb.Bet)
                    .ThenInclude(b => b.User)
                .Where(eb => eb.Bet.UserId == userId);

            if (state.HasValue)
            {
                query = query.Where(eb => eb.State == state.Value);
            }

            var bets = await query
                .OrderByDescending(eb => eb.CreatedAt)
                .Take(limit)
                .ToListAsync();

            var dtos = bets.Select(eb => new ExchangeBetDto
            {
                Id = eb.Id,
                BetId = eb.BetId,
                UserId = eb.Bet.UserId,
                Username = eb.Bet.User?.Username ?? "Unknown",
                Side = eb.Side,
                SideName = eb.Side.ToString(),
                ProposedOdds = eb.ProposedOdds,
                TotalStake = eb.TotalStake,
                MatchedStake = eb.MatchedStake,
                UnmatchedStake = eb.UnmatchedStake,
                State = eb.State,
                StateName = eb.State.ToString(),
                CreatedAt = eb.CreatedAt
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user's exchange bets");
            return StatusCode(500, new { message = "An error occurred while retrieving your bets" });
        }
    }
}
