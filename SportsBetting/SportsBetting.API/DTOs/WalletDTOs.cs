namespace SportsBetting.API.DTOs;

/// <summary>
/// Response DTO for wallet information
/// </summary>
public record WalletResponse(
    Guid Id,
    Guid UserId,
    decimal Balance,
    string Currency,
    decimal TotalDeposited,
    decimal TotalWithdrawn,
    decimal TotalBet,
    decimal TotalWon,
    decimal NetProfitLoss,
    DateTime LastUpdatedAt
);

/// <summary>
/// Request DTO for creating a wallet
/// </summary>
public record CreateWalletRequest(
    Guid? UserId,
    decimal? InitialBalance,
    string? Currency,
    string? Description
);

/// <summary>
/// Request DTO for depositing funds
/// </summary>
public record DepositRequest(
    decimal Amount,
    string? Description
);

/// <summary>
/// Request DTO for withdrawing funds
/// </summary>
public record WithdrawRequest(
    decimal Amount,
    string? Description
);

/// <summary>
/// Response DTO for transaction
/// </summary>
public record TransactionResponse(
    Guid Id,
    Guid UserId,
    string Type,
    decimal Amount,
    string Currency,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string Description,
    string Status,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
