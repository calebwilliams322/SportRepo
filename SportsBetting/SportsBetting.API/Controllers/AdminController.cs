using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBetting.API.DTOs;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace SportsBetting.API.Controllers;

/// <summary>
/// Admin-only endpoints for managing the platform
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "AdminOnly")] // All endpoints require Admin role
public class AdminController : ControllerBase
{
    private readonly SportsBettingDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(SportsBettingDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ============ USER MANAGEMENT ============

    /// <summary>
    /// Get all users in the system
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType<List<UserAdminResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserAdminResponse>>> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var users = await _context.Users
            .Include(u => u.Wallet)
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserAdminResponse
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Currency = u.Currency,
                Role = u.Role.ToString(),
                Status = u.Status.ToString(),
                EmailVerified = u.EmailVerified,
                WalletBalance = u.Wallet != null ? u.Wallet.Balance.Amount : 0,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();

        _logger.LogInformation("Admin {AdminId} retrieved {Count} users", GetCurrentUserId(), users.Count);
        return Ok(users);
    }

    /// <summary>
    /// Get detailed information about a specific user
    /// </summary>
    [HttpGet("users/{userId}")]
    [ProducesResponseType<UserDetailedAdminResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailedAdminResponse>> GetUserDetails(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        var betCount = await _context.Bets.CountAsync(b => b.UserId == userId);
        var totalWagered = await _context.Bets
            .Where(b => b.UserId == userId)
            .SumAsync(b => (decimal?)b.Stake.Amount) ?? 0;

        _logger.LogInformation("Admin {AdminId} viewed details for user {UserId}", GetCurrentUserId(), userId);

        return Ok(new UserDetailedAdminResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Currency = user.Currency,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            EmailVerified = user.EmailVerified,
            WalletId = user.Wallet?.Id,
            WalletBalance = user.Wallet?.Balance.Amount ?? 0,
            TotalBets = betCount,
            TotalWagered = totalWagered,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        });
    }

    /// <summary>
    /// Update a user's role
    /// </summary>
    [HttpPatch("users/{userId}/role")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateRoleRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var newRole))
        {
            return BadRequest(new { message = "Invalid role. Must be Customer, Support, or Admin" });
        }

        user.SetRole(newRole);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Admin {AdminId} changed user {UserId} role to {NewRole}",
            GetCurrentUserId(), userId, newRole);

        return Ok(new { message = $"User role updated to {newRole}" });
    }

    /// <summary>
    /// Suspend or activate a user account
    /// </summary>
    [HttpPatch("users/{userId}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromBody] UpdateStatusRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        switch (request.Action.ToLower())
        {
            case "suspend":
                user.Suspend();
                break;
            case "activate":
                user.Reactivate();
                break;
            case "close":
                user.Close();
                break;
            default:
                return BadRequest(new { message = "Invalid action. Must be 'suspend', 'activate', or 'close'" });
        }

        await _context.SaveChangesAsync();

        _logger.LogWarning("Admin {AdminId} changed user {UserId} status: {Action}",
            GetCurrentUserId(), userId, request.Action);

        return Ok(new { message = $"User status updated: {request.Action}" });
    }

    // ============ BETS MANAGEMENT ============

    /// <summary>
    /// Get all bets in the system
    /// </summary>
    [HttpGet("bets")]
    [ProducesResponseType<List<BetResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BetResponse>>> GetAllBets(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.Bets.Include(b => b.Selections).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<Domain.Enums.BetStatus>(status, true, out var betStatus))
        {
            query = query.Where(b => b.Status == betStatus);
        }

        var bets = await query
            .OrderByDescending(b => b.PlacedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("Admin {AdminId} retrieved {Count} bets", GetCurrentUserId(), bets.Count);

        return Ok(bets.Select(MapToBetResponse).ToList());
    }

    // ============ WALLETS MANAGEMENT ============

    /// <summary>
    /// Get all wallets in the system
    /// </summary>
    [HttpGet("wallets")]
    [ProducesResponseType<List<WalletAdminResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<WalletAdminResponse>>> GetAllWallets(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var wallets = await _context.Wallets
            .Include(w => w.User)
            .OrderByDescending(w => w.Balance.Amount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WalletAdminResponse
            {
                Id = w.Id,
                UserId = w.UserId,
                Username = w.User != null ? w.User.Username : "Unknown",
                Balance = w.Balance.Amount,
                Currency = w.Balance.Currency,
                TotalDeposited = w.TotalDeposited.Amount,
                TotalWithdrawn = w.TotalWithdrawn.Amount,
                TotalBet = w.TotalBet.Amount,
                TotalWon = w.TotalWon.Amount,
                NetProfitLoss = w.TotalWon.Amount - w.TotalBet.Amount
            })
            .ToListAsync();

        _logger.LogInformation("Admin {AdminId} retrieved {Count} wallets", GetCurrentUserId(), wallets.Count);
        return Ok(wallets);
    }

    // ============ STATISTICS ============

    /// <summary>
    /// Get platform statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType<PlatformStatsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformStatsResponse>> GetPlatformStats()
    {
        var totalWalletBalance = await _context.Wallets.SumAsync(w => (decimal?)w.Balance.Amount) ?? 0;

        // Load bets with payouts into memory first, then sum
        var betsWithPayouts = await _context.Bets
            .Where(b => b.ActualPayout != null)
            .ToListAsync();
        var totalPaidOut = betsWithPayouts.Sum(b => b.ActualPayout!.Value.Amount);

        var stats = new PlatformStatsResponse
        {
            TotalUsers = await _context.Users.CountAsync(),
            ActiveUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Active),
            SuspendedUsers = await _context.Users.CountAsync(u => u.Status == UserStatus.Suspended),
            TotalBets = await _context.Bets.CountAsync(),
            PendingBets = await _context.Bets.CountAsync(b => b.Status == Domain.Enums.BetStatus.Pending),
            SettledBets = await _context.Bets.CountAsync(b =>
                b.Status == Domain.Enums.BetStatus.Won ||
                b.Status == Domain.Enums.BetStatus.Lost ||
                b.Status == Domain.Enums.BetStatus.Pushed),
            WonBets = await _context.Bets.CountAsync(b => b.Status == Domain.Enums.BetStatus.Won),
            LostBets = await _context.Bets.CountAsync(b => b.Status == Domain.Enums.BetStatus.Lost),
            TotalWagered = await _context.Bets.SumAsync(b => (decimal?)b.Stake.Amount) ?? 0,
            TotalPaidOut = totalPaidOut,
            TotalWalletBalance = totalWalletBalance
        };

        _logger.LogInformation("Admin {AdminId} retrieved platform statistics", GetCurrentUserId());
        return Ok(stats);
    }

    // ============ HELPER METHODS ============

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user ID in token");
        }
        return userId;
    }

    private BetResponse MapToBetResponse(Domain.Entities.Bet bet)
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
}

// ============ ADMIN DTOs ============

public record UserAdminResponse
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool EmailVerified { get; init; }
    public decimal WalletBalance { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}

public record UserDetailedAdminResponse : UserAdminResponse
{
    public Guid? WalletId { get; init; }
    public int TotalBets { get; init; }
    public decimal TotalWagered { get; init; }
}

public record WalletAdminResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal TotalDeposited { get; init; }
    public decimal TotalWithdrawn { get; init; }
    public decimal TotalBet { get; init; }
    public decimal TotalWon { get; init; }
    public decimal NetProfitLoss { get; init; }
}

public record PlatformStatsResponse
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int SuspendedUsers { get; init; }
    public int TotalBets { get; init; }
    public int PendingBets { get; init; }
    public int SettledBets { get; init; }
    public int WonBets { get; init; }
    public int LostBets { get; init; }
    public decimal TotalWagered { get; init; }
    public decimal TotalPaidOut { get; init; }
    public decimal TotalWalletBalance { get; init; }
}

public record UpdateRoleRequest
{
    public string Role { get; init; } = string.Empty;
}

public record UpdateStatusRequest
{
    public string Action { get; init; } = string.Empty;
}
