using SportsBetting.Domain.Exceptions;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a user's wallet/balance for sports betting
/// </summary>
public class Wallet
{
    public Guid Id { get; }
    public Guid UserId { get; private set; }

    /// <summary>
    /// Current available balance
    /// </summary>
    public Money Balance { get; private set; }

    /// <summary>
    /// Lifetime total deposited
    /// </summary>
    public Money TotalDeposited { get; private set; }

    /// <summary>
    /// Lifetime total withdrawn
    /// </summary>
    public Money TotalWithdrawn { get; private set; }

    /// <summary>
    /// Lifetime total amount bet (stake sum)
    /// </summary>
    public Money TotalBet { get; private set; }

    /// <summary>
    /// Lifetime total winnings (payout sum)
    /// </summary>
    public Money TotalWon { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime LastUpdatedAt { get; private set; }

    /// <summary>
    /// Concurrency token for optimistic locking (prevents double-spend)
    /// Maps to PostgreSQL's xmin system column (transaction ID)
    /// Nullable to support SQLite tests (which don't have xmin)
    /// </summary>
    public uint? RowVersion { get; private set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public User? User { get; private set; }

    // Private parameterless constructor for EF Core
    private Wallet()
    {
        Balance = Money.Zero();
        TotalDeposited = Money.Zero();
        TotalWithdrawn = Money.Zero();
        TotalBet = Money.Zero();
        TotalWon = Money.Zero();
    }

    public Wallet(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        Id = Guid.NewGuid();
        UserId = user.Id;
        User = user;

        var currency = user.Currency;
        Balance = Money.Zero(currency);
        TotalDeposited = Money.Zero(currency);
        TotalWithdrawn = Money.Zero(currency);
        TotalBet = Money.Zero(currency);
        TotalWon = Money.Zero(currency);

        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;

        // Set wallet on user
        user.SetWallet(this);
    }

    /// <summary>
    /// Deposit funds into the wallet
    /// </summary>
    public void Deposit(Money amount)
    {
        ValidateAmount(amount);
        ValidateCurrency(amount);

        Balance += amount;
        TotalDeposited += amount;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Withdraw funds from the wallet
    /// </summary>
    public void Withdraw(Money amount)
    {
        ValidateAmount(amount);
        ValidateCurrency(amount);

        if (!HasSufficientBalance(amount))
            throw new InsufficientFundsException($"Insufficient balance. Available: {Balance}, Requested: {amount}");

        Balance -= amount;
        TotalWithdrawn += amount;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deduct stake when a bet is placed
    /// </summary>
    internal void DeductStake(Money stake)
    {
        ValidateAmount(stake);
        ValidateCurrency(stake);

        if (!HasSufficientBalance(stake))
            throw new InsufficientFundsException($"Insufficient balance to place bet. Available: {Balance}, Required: {stake}");

        Balance -= stake;
        TotalBet += stake;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Credit winnings when a bet is settled
    /// </summary>
    internal void CreditPayout(Money payout)
    {
        ValidateAmount(payout);
        ValidateCurrency(payout);

        Balance += payout;
        TotalWon += payout;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Refund stake (for void/push/cancelled bets)
    /// Note: This just credits balance, doesn't affect TotalBet
    /// </summary>
    internal void RefundStake(Money stake)
    {
        ValidateAmount(stake);
        ValidateCurrency(stake);

        Balance += stake;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if wallet has sufficient balance
    /// </summary>
    public bool HasSufficientBalance(Money amount)
    {
        ValidateCurrency(amount);
        return Balance >= amount;
    }

    private void ValidateAmount(Money amount)
    {
        if (amount.Amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
    }

    private void ValidateCurrency(Money amount)
    {
        if (amount.Currency != Balance.Currency)
            throw new ArgumentException(
                $"Currency mismatch. Wallet uses {Balance.Currency}, provided {amount.Currency}",
                nameof(amount));
    }

    /// <summary>
    /// Net profit/loss (winnings - bets)
    /// </summary>
    public Money NetProfitLoss => TotalWon - TotalBet;

    /// <summary>
    /// Return on Investment percentage
    /// </summary>
    public decimal ROI
    {
        get
        {
            if (TotalBet.Amount == 0)
                return 0;

            return (NetProfitLoss.Amount / TotalBet.Amount) * 100;
        }
    }

    public override string ToString() => $"Wallet - Balance: {Balance}, Net P/L: {NetProfitLoss}";
}
