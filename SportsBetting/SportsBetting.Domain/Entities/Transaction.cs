using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a financial transaction in the system
/// Provides complete audit trail for all wallet operations
/// </summary>
public class Transaction
{
    public Guid Id { get; }
    public Guid UserId { get; private set; }
    public TransactionType Type { get; private set; }

    /// <summary>
    /// Transaction amount
    /// </summary>
    public Money Amount { get; private set; }

    /// <summary>
    /// Wallet balance before this transaction
    /// </summary>
    public Money BalanceBefore { get; private set; }

    /// <summary>
    /// Wallet balance after this transaction
    /// </summary>
    public Money BalanceAfter { get; private set; }

    /// <summary>
    /// Reference to related entity (e.g., BetId if this is a bet-related transaction)
    /// </summary>
    public Guid? ReferenceId { get; private set; }

    /// <summary>
    /// Description/notes for this transaction
    /// </summary>
    public string Description { get; private set; }

    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public User? User { get; private set; }

    // Private parameterless constructor for EF Core
    private Transaction()
    {
        // Initialize required non-nullable string properties
        Description = string.Empty;
        // Initialize Money properties
        Amount = Money.Zero();
        BalanceBefore = Money.Zero();
        BalanceAfter = Money.Zero();
    }

    private Transaction(
        User user,
        TransactionType type,
        Money amount,
        Money balanceBefore,
        Money balanceAfter,
        string description,
        TransactionStatus status,
        Guid? referenceId = null)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (amount.Amount <= 0)
            throw new ArgumentException("Transaction amount must be positive", nameof(amount));

        // Validate currency consistency
        if (amount.Currency != balanceBefore.Currency || amount.Currency != balanceAfter.Currency)
            throw new ArgumentException("All amounts must be in the same currency");

        Id = Guid.NewGuid();
        UserId = user.Id;
        User = user;
        Type = type;
        Amount = amount;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
        ReferenceId = referenceId;
        Description = description;
        Status = status;
        CreatedAt = DateTime.UtcNow;
        CompletedAt = status == TransactionStatus.Completed ? DateTime.UtcNow : null;
    }

    public Transaction(
        User user,
        TransactionType type,
        Money amount,
        Money balanceBefore,
        Money balanceAfter,
        string description,
        Guid? referenceId = null)
        : this(user, type, amount, balanceBefore, balanceAfter, description, TransactionStatus.Completed, referenceId)
    {
    }

    /// <summary>
    /// Create a pending transaction (for operations that need approval)
    /// </summary>
    public static Transaction CreatePending(
        User user,
        TransactionType type,
        Money amount,
        string description,
        Guid? referenceId = null)
    {
        // Create with zero balances temporarily - will be updated on completion
        return new Transaction(
            user,
            type,
            amount,
            Money.Zero(amount.Currency),
            Money.Zero(amount.Currency),
            description,
            TransactionStatus.Pending,
            referenceId);
    }

    /// <summary>
    /// Complete a pending transaction
    /// </summary>
    public void Complete(Money balanceBefore, Money balanceAfter)
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot complete transaction in {Status} status");

        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
        Status = TransactionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Fail/reject a pending transaction
    /// </summary>
    public void Fail(string reason)
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException($"Cannot fail transaction in {Status} status");

        Description += $" - FAILED: {reason}";
        Status = TransactionStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }

    public bool IsCompleted => Status == TransactionStatus.Completed;
    public bool IsPending => Status == TransactionStatus.Pending;
    public bool IsFailed => Status == TransactionStatus.Failed;

    public override string ToString() => $"{Type} - {Amount} - {Description}";
}

/// <summary>
/// Type of transaction
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// User deposited funds
    /// </summary>
    Deposit = 0,

    /// <summary>
    /// User withdrew funds
    /// </summary>
    Withdrawal = 1,

    /// <summary>
    /// Stake deducted for placing a bet
    /// </summary>
    BetPlaced = 2,

    /// <summary>
    /// Winnings credited from settled bet
    /// </summary>
    BetPayout = 3,

    /// <summary>
    /// Stake refunded (void/push/cancelled bet)
    /// </summary>
    BetRefund = 4,

    /// <summary>
    /// Administrative adjustment
    /// </summary>
    Adjustment = 5
}

/// <summary>
/// Transaction status
/// </summary>
public enum TransactionStatus
{
    /// <summary>
    /// Transaction is pending approval/processing
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Transaction completed successfully
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Transaction failed or was rejected
    /// </summary>
    Failed = 2
}
