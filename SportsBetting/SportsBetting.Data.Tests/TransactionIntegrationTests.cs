using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Tests;

public class TransactionIntegrationTests : IDisposable
{
    private readonly SportsBettingDbContext _context;
    private readonly WalletService _walletService;

    public TransactionIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new SportsBettingDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
        _walletService = new WalletService();
    }

    [Fact]
    public void DepositCreatesTransactionRecord()
    {
        // Arrange
        var user = new User("txuser", "tx@example.com", "password");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act
        var transaction = _walletService.Deposit(user, new Money(100m, "USD"));
        _context.Transactions.Add(transaction);
        _context.SaveChanges();

        // Assert
        var savedTransaction = _context.Transactions.FirstOrDefault(t => t.Id == transaction.Id);
        Assert.NotNull(savedTransaction);
        Assert.Equal(TransactionType.Deposit, savedTransaction.Type);
        Assert.Equal(100m, savedTransaction.Amount.Amount);
        Assert.Equal(0m, savedTransaction.BalanceBefore.Amount);
        Assert.Equal(100m, savedTransaction.BalanceAfter.Amount);
        Assert.Equal(TransactionStatus.Completed, savedTransaction.Status);
    }

    [Fact]
    public void WithdrawalCreatesTransactionRecord()
    {
        // Arrange
        var user = new User("withdrawuser", "withdraw@example.com", "password");
        var wallet = new Wallet(user);
        wallet.Deposit(new Money(100m, "USD"));
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act
        var transaction = _walletService.Withdraw(user, new Money(30m, "USD"));
        _context.Transactions.Add(transaction);
        _context.SaveChanges();

        // Assert
        var savedTransaction = _context.Transactions.FirstOrDefault(t => t.Id == transaction.Id);
        Assert.NotNull(savedTransaction);
        Assert.Equal(TransactionType.Withdrawal, savedTransaction.Type);
        Assert.Equal(30m, savedTransaction.Amount.Amount);
        Assert.Equal(100m, savedTransaction.BalanceBefore.Amount);
        Assert.Equal(70m, savedTransaction.BalanceAfter.Amount);
    }

    [Fact]
    public void CanQueryTransactionsByUser()
    {
        // Arrange
        var user = new User("multiuser", "multi@example.com", "password");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);

        var tx1 = _walletService.Deposit(user, new Money(100m, "USD"));
        var tx2 = _walletService.Deposit(user, new Money(50m, "USD"));
        var tx3 = _walletService.Withdraw(user, new Money(25m, "USD"));

        _context.Transactions.AddRange(tx1, tx2, tx3);
        _context.SaveChanges();

        // Act
        var userTransactions = _context.Transactions
            .Where(t => t.UserId == user.Id)
            .OrderBy(t => t.CreatedAt)
            .ToList();

        // Assert
        Assert.Equal(3, userTransactions.Count);
        Assert.Equal(TransactionType.Deposit, userTransactions[0].Type);
        Assert.Equal(TransactionType.Deposit, userTransactions[1].Type);
        Assert.Equal(TransactionType.Withdrawal, userTransactions[2].Type);
    }

    [Fact]
    public void TransactionTimestampsArePersisted()
    {
        // Arrange
        var user = new User("timeuser", "time@example.com", "password");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act
        var beforeTransaction = DateTime.UtcNow;
        var transaction = _walletService.Deposit(user, new Money(100m, "USD"));
        _context.Transactions.Add(transaction);
        _context.SaveChanges();
        var afterTransaction = DateTime.UtcNow;

        // Assert
        var savedTransaction = _context.Transactions.Find(transaction.Id);
        Assert.NotNull(savedTransaction);
        Assert.True(savedTransaction.CreatedAt >= beforeTransaction);
        Assert.True(savedTransaction.CreatedAt <= afterTransaction);
        Assert.NotNull(savedTransaction.CompletedAt);
        Assert.True(savedTransaction.CompletedAt >= beforeTransaction);
    }

    [Fact]
    public void MoneyValuesArePersistedCorrectly()
    {
        // Arrange
        var user = new User("moneyuser", "money@example.com", "password");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act
        var transaction = _walletService.Deposit(user, new Money(123.45m, "USD"));
        _context.Transactions.Add(transaction);
        _context.SaveChanges();

        _context.Entry(transaction).State = EntityState.Detached;
        var retrieved = _context.Transactions.Find(transaction.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(123.45m, retrieved.Amount.Amount);
        Assert.Equal("USD", retrieved.Amount.Currency);
        Assert.Equal(0m, retrieved.BalanceBefore.Amount);
        Assert.Equal(123.45m, retrieved.BalanceAfter.Amount);
    }

    [Fact]
    public void CanFilterTransactionsByType()
    {
        // Arrange
        var user = new User("filteruser", "filter@example.com", "password");
        var wallet = new Wallet(user);
        wallet.Deposit(new Money(200m, "USD"));
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);

        var deposit = _walletService.Deposit(user, new Money(100m, "USD"));
        var withdrawal = _walletService.Withdraw(user, new Money(50m, "USD"));

        _context.Transactions.AddRange(deposit, withdrawal);
        _context.SaveChanges();

        // Act
        var deposits = _context.Transactions
            .Where(t => t.UserId == user.Id && t.Type == TransactionType.Deposit)
            .ToList();

        var withdrawals = _context.Transactions
            .Where(t => t.UserId == user.Id && t.Type == TransactionType.Withdrawal)
            .ToList();

        // Assert
        Assert.Single(deposits);
        Assert.Single(withdrawals);
        Assert.Equal(100m, deposits[0].Amount.Amount);
        Assert.Equal(50m, withdrawals[0].Amount.Amount);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
