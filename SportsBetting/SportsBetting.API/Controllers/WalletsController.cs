using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsBetting.API.DTOs;
using SportsBetting.Data;
using SportsBetting.Domain.Exceptions;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;
using System.Security.Claims;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // Require authentication for all endpoints in this controller
public class WalletsController : ControllerBase
{
    private readonly SportsBettingDbContext _context;
    private readonly WalletService _walletService;
    private readonly ILogger<WalletsController> _logger;

    public WalletsController(
        SportsBettingDbContext context,
        WalletService walletService,
        ILogger<WalletsController> logger)
    {
        _context = context;
        _walletService = walletService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new wallet for a user
    /// </summary>
    [HttpPost]
    [ProducesResponseType<WalletResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WalletResponse>> CreateWallet([FromBody] CreateWalletRequest request)
    {
        // Get the user from the request or use current user
        var userId = request.UserId ?? GetCurrentUserId();

        // Check if wallet already exists for this user
        var existingWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (existingWallet != null)
        {
            return Conflict(new { message = "Wallet already exists for this user" });
        }

        // Only admins can create wallets for other users
        if (request.UserId != null && request.UserId != GetCurrentUserId() && !IsAdmin())
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = $"User {userId} not found" });
        }

        // Create wallet using the User constructor (wallet is created automatically)
        var wallet = new SportsBetting.Domain.Entities.Wallet(user);
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        decimal depositAmount = 0m;
        string currency = request.Currency ?? "USD";

        // If there's an initial balance, deposit it
        if (request.InitialBalance > 0)
        {
            // Reload the user with wallet included
            user = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == userId);

            var initialBalance = new Money(request.InitialBalance.Value, currency);
            var transaction = _walletService.Deposit(
                user!,
                initialBalance,
                request.Description ?? "Initial deposit"
            );
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            depositAmount = request.InitialBalance.Value;
        }

        _logger.LogInformation("Created wallet {WalletId} for user {UserId} with initial balance {Amount} {Currency}",
            wallet.Id, userId, depositAmount, currency);

        return CreatedAtAction(
            nameof(GetWallet),
            new { id = wallet.Id },
            new WalletResponse(
                wallet.Id,
                wallet.UserId,
                wallet.Balance.Amount,
                wallet.Balance.Currency,
                wallet.TotalDeposited.Amount,
                wallet.TotalWithdrawn.Amount,
                wallet.TotalBet.Amount,
                wallet.TotalWon.Amount,
                wallet.TotalWon.Amount - wallet.TotalBet.Amount,
                wallet.LastUpdatedAt
            )
        );
    }

    /// <summary>
    /// Get wallet by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType<WalletResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WalletResponse>> GetWallet(Guid id)
    {
        var wallet = await _context.Wallets.FindAsync(id);

        if (wallet == null)
        {
            return NotFound(new { message = $"Wallet {id} not found" });
        }

        // AUTHORIZATION: Check if user can access this wallet
        if (!CanAccessWallet(wallet.UserId))
        {
            _logger.LogWarning("User {CurrentUser} attempted to access wallet {WalletId} owned by {WalletOwner}",
                GetCurrentUserId(), id, wallet.UserId);
            return Forbid();
        }

        return Ok(new WalletResponse(
            wallet.Id,
            wallet.UserId,
            wallet.Balance.Amount,
            wallet.Balance.Currency,
            wallet.TotalDeposited.Amount,
            wallet.TotalWithdrawn.Amount,
            wallet.TotalBet.Amount,
            wallet.TotalWon.Amount,
            wallet.TotalWon.Amount - wallet.TotalBet.Amount, // Net P/L can be negative
            wallet.LastUpdatedAt
        ));
    }

    /// <summary>
    /// Get wallet by user ID
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType<WalletResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WalletResponse>> GetWalletByUserId(Guid userId)
    {
        // AUTHORIZATION: Check if user can access this wallet
        if (!CanAccessWallet(userId))
        {
            _logger.LogWarning("User {CurrentUser} attempted to access wallet for user {RequestedUser}",
                GetCurrentUserId(), userId);
            return Forbid();
        }

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            return NotFound(new { message = $"Wallet for user {userId} not found" });
        }

        return Ok(new WalletResponse(
            wallet.Id,
            wallet.UserId,
            wallet.Balance.Amount,
            wallet.Balance.Currency,
            wallet.TotalDeposited.Amount,
            wallet.TotalWithdrawn.Amount,
            wallet.TotalBet.Amount,
            wallet.TotalWon.Amount,
            wallet.TotalWon.Amount - wallet.TotalBet.Amount, // Net P/L can be negative
            wallet.LastUpdatedAt
        ));
    }

    /// <summary>
    /// Deposit funds into wallet
    /// </summary>
    [HttpPost("{id}/deposit")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionResponse>> Deposit(Guid id, [FromBody] DepositRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Deposit amount must be greater than zero" });
        }

        // Retry logic for optimistic concurrency
        const int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                // Reload user and wallet on each retry to get latest RowVersion
                var user = await _context.Users
                    .Include(u => u.Wallet)
                    .FirstOrDefaultAsync(u => u.Wallet!.Id == id);

                if (user == null || user.Wallet == null)
                {
                    return NotFound(new { message = $"Wallet {id} not found" });
                }

                // AUTHORIZATION: Check if user can modify this wallet
                if (!CanModifyWallet(user.Id))
                {
                    _logger.LogWarning("User {CurrentUser} attempted to deposit to wallet {WalletId} owned by {WalletOwner}",
                        GetCurrentUserId(), id, user.Id);
                    return Forbid();
                }

                var transaction = _walletService.Deposit(
                    user,
                    new Money(request.Amount, user.Wallet.Balance.Currency),
                    request.Description ?? "Deposit"
                );

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deposited {Amount} to wallet {WalletId} (attempt {Attempt})",
                    request.Amount, id, retryCount + 1);

                return Ok(new TransactionResponse(
                    transaction.Id,
                    transaction.UserId,
                    transaction.Type.ToString(),
                    transaction.Amount.Amount,
                    transaction.Amount.Currency,
                    transaction.BalanceBefore.Amount,
                    transaction.BalanceAfter.Amount,
                    transaction.Description,
                    transaction.Status.ToString(),
                    transaction.CreatedAt,
                    transaction.CompletedAt
                ));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                _logger.LogWarning("Concurrency conflict on wallet {WalletId} deposit, retry {Retry}/{MaxRetries}",
                    id, retryCount, maxRetries);

                // Clear the context to avoid tracking conflicts
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, "Max retries exceeded for wallet {WalletId} deposit", id);
                    return Conflict(new { message = "Unable to process deposit due to concurrent updates. Please try again." });
                }

                // Small delay before retry to reduce contention
                await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount));
            }
        }

        return Conflict(new { message = "Unable to process deposit. Please try again." });
    }

    /// <summary>
    /// Withdraw funds from wallet
    /// </summary>
    [HttpPost("{id}/withdraw")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TransactionResponse>> Withdraw(Guid id, [FromBody] WithdrawRequest request)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Withdrawal amount must be greater than zero" });
        }

        // Retry logic for optimistic concurrency
        const int maxRetries = 3;
        int retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                // Reload user and wallet on each retry to get latest RowVersion
                var user = await _context.Users
                    .Include(u => u.Wallet)
                    .FirstOrDefaultAsync(u => u.Wallet!.Id == id);

                if (user == null || user.Wallet == null)
                {
                    return NotFound(new { message = $"Wallet {id} not found" });
                }

                // AUTHORIZATION: Check if user can modify this wallet (CRITICAL for withdrawals!)
                if (!CanModifyWallet(user.Id))
                {
                    _logger.LogWarning("SECURITY: User {CurrentUser} attempted to withdraw from wallet {WalletId} owned by {WalletOwner}",
                        GetCurrentUserId(), id, user.Id);
                    return Forbid();
                }

                var transaction = _walletService.Withdraw(
                    user,
                    new Money(request.Amount, user.Wallet.Balance.Currency),
                    request.Description ?? "Withdrawal"
                );

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Withdrew {Amount} from wallet {WalletId} (attempt {Attempt})",
                    request.Amount, id, retryCount + 1);

                return Ok(new TransactionResponse(
                    transaction.Id,
                    transaction.UserId,
                    transaction.Type.ToString(),
                    transaction.Amount.Amount,
                    transaction.Amount.Currency,
                    transaction.BalanceBefore.Amount,
                    transaction.BalanceAfter.Amount,
                    transaction.Description,
                    transaction.Status.ToString(),
                    transaction.CreatedAt,
                    transaction.CompletedAt
                ));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                retryCount++;
                _logger.LogWarning("Concurrency conflict on wallet {WalletId} withdrawal, retry {Retry}/{MaxRetries}",
                    id, retryCount, maxRetries);

                // Clear the context to avoid tracking conflicts
                foreach (var entry in _context.ChangeTracker.Entries())
                {
                    entry.State = EntityState.Detached;
                }

                if (retryCount >= maxRetries)
                {
                    _logger.LogError(ex, "Max retries exceeded for wallet {WalletId} withdrawal", id);
                    return Conflict(new { message = "Unable to process withdrawal due to concurrent updates. Please try again." });
                }

                // Small delay before retry to reduce contention
                await Task.Delay(TimeSpan.FromMilliseconds(50 * retryCount));
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

        return Conflict(new { message = "Unable to process withdrawal. Please try again." });
    }

    /// <summary>
    /// Get transaction history for a wallet
    /// </summary>
    [HttpGet("{id}/transactions")]
    [ProducesResponseType<List<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TransactionResponse>>> GetTransactions(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var wallet = await _context.Wallets.FindAsync(id);
        if (wallet == null)
        {
            return NotFound(new { message = $"Wallet {id} not found" });
        }

        // AUTHORIZATION: Check if user can access this wallet's transactions
        if (!CanAccessWallet(wallet.UserId))
        {
            _logger.LogWarning("User {CurrentUser} attempted to access transactions for wallet {WalletId} owned by {WalletOwner}",
                GetCurrentUserId(), id, wallet.UserId);
            return Forbid();
        }

        var transactions = await _context.Transactions
            .Where(t => t.UserId == wallet.UserId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionResponse(
                t.Id,
                t.UserId,
                t.Type.ToString(),
                t.Amount.Amount,
                t.Amount.Currency,
                t.BalanceBefore.Amount,
                t.BalanceAfter.Amount,
                t.Description,
                t.Status.ToString(),
                t.CreatedAt,
                t.CompletedAt
            ))
            .ToListAsync();

        return Ok(transactions);
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
    /// Check if the current user is support or admin
    /// </summary>
    private bool IsSupportOrAdmin()
    {
        return User.IsInRole("Support") || User.IsInRole("Admin");
    }

    /// <summary>
    /// Check if the current user can access the specified wallet
    /// Admins and Support can access any wallet, regular users can only access their own
    /// </summary>
    private bool CanAccessWallet(Guid walletUserId)
    {
        if (IsSupportOrAdmin())
            return true;

        var currentUserId = GetCurrentUserId();
        return currentUserId == walletUserId;
    }

    /// <summary>
    /// Check if the current user can modify the specified wallet (deposit/withdraw)
    /// Only admins and the wallet owner can modify
    /// </summary>
    private bool CanModifyWallet(Guid walletUserId)
    {
        if (IsAdmin())
            return true;

        var currentUserId = GetCurrentUserId();
        return currentUserId == walletUserId;
    }
}
