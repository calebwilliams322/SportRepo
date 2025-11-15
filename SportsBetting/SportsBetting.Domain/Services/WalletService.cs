using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Exceptions;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Services;

/// <summary>
/// Domain service for wallet operations
/// Coordinates balance updates with transaction logging
/// </summary>
public class WalletService
{
    private readonly List<Transaction> _transactions;

    public WalletService()
    {
        _transactions = new List<Transaction>();
    }

    /// <summary>
    /// Deposit funds into a user's wallet
    /// </summary>
    public Transaction Deposit(User user, Money amount, string description = "Deposit")
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (user.Wallet == null)
            throw new InvalidOperationException("User does not have a wallet");

        var balanceBefore = user.Wallet.Balance;
        user.Wallet.Deposit(amount);
        var balanceAfter = user.Wallet.Balance;

        var transaction = new Transaction(
            user,
            TransactionType.Deposit,
            amount,
            balanceBefore,
            balanceAfter,
            description
        );

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Withdraw funds from a user's wallet
    /// </summary>
    public Transaction Withdraw(User user, Money amount, string description = "Withdrawal")
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (user.Wallet == null)
            throw new InvalidOperationException("User does not have a wallet");

        var balanceBefore = user.Wallet.Balance;
        user.Wallet.Withdraw(amount); // This will throw if insufficient funds
        var balanceAfter = user.Wallet.Balance;

        var transaction = new Transaction(
            user,
            TransactionType.Withdrawal,
            amount,
            balanceBefore,
            balanceAfter,
            description
        );

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Place a bet - deduct stake from wallet and create transaction
    /// </summary>
    public Transaction PlaceBet(User user, Bet bet)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (bet == null)
            throw new ArgumentNullException(nameof(bet));

        if (user.Wallet == null)
            throw new InvalidOperationException("User does not have a wallet");

        if (bet.UserId != user.Id)
            throw new InvalidOperationException("Bet does not belong to this user");

        var balanceBefore = user.Wallet.Balance;
        user.Wallet.DeductStake(bet.Stake); // This will throw if insufficient funds
        var balanceAfter = user.Wallet.Balance;

        var transaction = new Transaction(
            user,
            TransactionType.BetPlaced,
            bet.Stake,
            balanceBefore,
            balanceAfter,
            $"Bet placed: {bet.TicketNumber}",
            bet.Id
        );

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Settle a bet - credit payout to wallet and create transaction
    /// </summary>
    public Transaction? SettleBet(User user, Bet bet)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (bet == null)
            throw new ArgumentNullException(nameof(bet));

        if (user.Wallet == null)
            throw new InvalidOperationException("User does not have a wallet");

        if (bet.UserId != user.Id)
            throw new InvalidOperationException("Bet does not belong to this user");

        if (!bet.IsSettled)
            throw new InvalidOperationException("Bet is not settled yet");

        if (bet.ActualPayout == null)
            throw new InvalidOperationException("Bet has no payout amount");

        var payout = bet.ActualPayout.Value; // Get the Money struct from nullable

        // Only create transaction if there's a payout (lost bets have $0 payout)
        if (payout.Amount > 0)
        {
            var balanceBefore = user.Wallet.Balance;
            user.Wallet.CreditPayout(payout);
            var balanceAfter = user.Wallet.Balance;

            var description = bet.Status switch
            {
                BetStatus.Won => $"Bet won: {bet.TicketNumber}",
                BetStatus.Pushed => $"Bet pushed (refund): {bet.TicketNumber}",
                BetStatus.Void => $"Bet voided (refund): {bet.TicketNumber}",
                _ => $"Bet settled: {bet.TicketNumber}"
            };

            var transactionType = bet.Status == BetStatus.Won
                ? TransactionType.BetPayout
                : TransactionType.BetRefund;

            var transaction = new Transaction(
                user,
                transactionType,
                payout,
                balanceBefore,
                balanceAfter,
                description,
                bet.Id
            );

            _transactions.Add(transaction);
            return transaction;
        }

        return null; // No transaction for lost bets
    }

    /// <summary>
    /// Purchase a LineLock - deduct fee from wallet
    /// </summary>
    public Transaction PurchaseLineLock(User user, LineLock lineLock)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (lineLock == null)
            throw new ArgumentNullException(nameof(lineLock));

        if (user.Wallet == null)
            throw new InvalidOperationException("User does not have a wallet");

        if (lineLock.UserId != user.Id)
            throw new InvalidOperationException("LineLock does not belong to this user");

        var balanceBefore = user.Wallet.Balance;
        user.Wallet.DeductStake(lineLock.LockFee); // Reuse DeductStake for consistency
        var balanceAfter = user.Wallet.Balance;

        var transaction = new Transaction(
            user,
            TransactionType.BetPlaced, // Or create a new type LineLockPurchase
            lineLock.LockFee,
            balanceBefore,
            balanceAfter,
            $"LineLock purchased: {lineLock.LockNumber}",
            lineLock.Id
        );

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Administrative balance adjustment
    /// </summary>
    public Transaction AdjustBalance(User user, Money amount, string reason)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (user.Wallet == null)
            throw new InvalidOperationException("User does not have a wallet");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Adjustment reason is required", nameof(reason));

        var balanceBefore = user.Wallet.Balance;

        if (amount.Amount > 0)
        {
            user.Wallet.Deposit(amount);
        }
        else
        {
            user.Wallet.Withdraw(new Money(Math.Abs(amount.Amount), amount.Currency));
        }

        var balanceAfter = user.Wallet.Balance;

        var transaction = new Transaction(
            user,
            TransactionType.Adjustment,
            new Money(Math.Abs(amount.Amount), amount.Currency),
            balanceBefore,
            balanceAfter,
            $"Admin adjustment: {reason}"
        );

        _transactions.Add(transaction);
        return transaction;
    }

    /// <summary>
    /// Get all transactions (for testing/debugging - in production this would be in a repository)
    /// </summary>
    public IReadOnlyList<Transaction> GetTransactions() => _transactions.AsReadOnly();

    /// <summary>
    /// Get transactions for a specific user
    /// </summary>
    public IReadOnlyList<Transaction> GetUserTransactions(Guid userId)
    {
        return _transactions.Where(t => t.UserId == userId).ToList().AsReadOnly();
    }
}
