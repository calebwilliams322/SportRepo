using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Tests;

public class UserWalletIntegrationTests : IDisposable
{
    private readonly SportsBettingDbContext _context;

    public UserWalletIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SportsBettingDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new SportsBettingDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void CanCreateAndRetrieveUser()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "hashedpassword123");

        // Act
        _context.Users.Add(user);
        _context.SaveChanges();

        var retrievedUser = _context.Users.FirstOrDefault(u => u.Username == "testuser");

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.Equal("testuser", retrievedUser.Username);
        Assert.Equal("test@example.com", retrievedUser.Email);
        Assert.Equal("hashedpassword123", retrievedUser.PasswordHash);
        Assert.Equal("USD", retrievedUser.Currency);
        Assert.Equal(UserStatus.Active, retrievedUser.Status);
    }

    [Fact]
    public void CanCreateWalletWithUser()
    {
        // Arrange
        var user = new User("walletuser", "wallet@example.com", "password");
        var wallet = new Wallet(user);

        // Act
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        var retrievedWallet = _context.Wallets
            .Include(w => w.User)
            .FirstOrDefault(w => w.UserId == user.Id);

        // Assert
        Assert.NotNull(retrievedWallet);
        Assert.Equal(user.Id, retrievedWallet.UserId);
        Assert.NotNull(retrievedWallet.User);
        Assert.Equal("walletuser", retrievedWallet.User.Username);
        Assert.Equal(0m, retrievedWallet.Balance.Amount);
        Assert.Equal("USD", retrievedWallet.Balance.Currency);
    }

    [Fact]
    public void WalletBalanceUpdatesArePersisted()
    {
        // Arrange
        var user = new User("balanceuser", "balance@example.com", "password");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act
        wallet.Deposit(new Money(100m, "USD"));
        _context.SaveChanges();

        // Clear context to force reload from database
        _context.Entry(wallet).State = EntityState.Detached;

        var retrievedWallet = _context.Wallets.Find(wallet.Id);

        // Assert
        Assert.NotNull(retrievedWallet);
        Assert.Equal(100m, retrievedWallet.Balance.Amount);
        Assert.Equal(100m, retrievedWallet.TotalDeposited.Amount);
    }

    [Fact]
    public void UserStatusChangesArePersisted()
    {
        // Arrange
        var user = new User("statususer", "status@example.com", "password");
        _context.Users.Add(user);
        _context.SaveChanges();

        // Act
        user.Suspend();
        _context.SaveChanges();

        _context.Entry(user).State = EntityState.Detached;
        var retrievedUser = _context.Users.Find(user.Id);

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.Equal(UserStatus.Suspended, retrievedUser.Status);
    }

    [Fact]
    public void DeletingUserCascadesDeleteToWallet()
    {
        // Arrange
        var user = new User("cascadeuser", "cascade@example.com", "password");
        var wallet = new Wallet(user);
        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();
        var walletId = wallet.Id;

        // Act
        _context.Users.Remove(user);
        _context.SaveChanges();

        // Assert
        var deletedWallet = _context.Wallets.Find(walletId);
        Assert.Null(deletedWallet);
    }

    [Fact]
    public void CanQueryUserWithWalletIncluded()
    {
        // Arrange
        var user = new User("includeuser", "include@example.com", "password");
        var wallet = new Wallet(user);
        wallet.Deposit(new Money(50m, "USD"));

        _context.Users.Add(user);
        _context.Wallets.Add(wallet);
        _context.SaveChanges();

        // Act - AutoInclude should load wallet automatically
        _context.Entry(user).State = EntityState.Detached;
        var retrievedUser = _context.Users.FirstOrDefault(u => u.Id == user.Id);

        // Assert
        Assert.NotNull(retrievedUser);
        Assert.NotNull(retrievedUser.Wallet);
        Assert.Equal(50m, retrievedUser.Wallet.Balance.Amount);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
