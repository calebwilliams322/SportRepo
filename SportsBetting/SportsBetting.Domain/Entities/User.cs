using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a user account in the sports betting system
/// </summary>
public class User
{
    public Guid Id { get; }
    public string Username { get; private set; }
    public string Email { get; private set; }

    /// <summary>
    /// Hashed password (never store plain text)
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// User's preferred currency for betting
    /// </summary>
    public string Currency { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? LastLoginAt { get; private set; }

    public UserStatus Status { get; private set; }

    /// <summary>
    /// Navigation property to user's wallet
    /// </summary>
    public Wallet? Wallet { get; private set; }

    // Private parameterless constructor for EF Core
    private User()
    {
        // Initialize required non-nullable string properties
        Username = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        Currency = string.Empty;
    }

    public User(string username, string email, string passwordHash, string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        if (username.Length < 3 || username.Length > 50)
            throw new ArgumentException("Username must be between 3 and 50 characters", nameof(username));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a valid 3-letter ISO code", nameof(currency));

        Id = Guid.NewGuid();
        Username = username;
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        Currency = currency.ToUpperInvariant();
        CreatedAt = DateTime.UtcNow;
        Status = UserStatus.Active;
    }

    /// <summary>
    /// Update user's email address
    /// </summary>
    public void UpdateEmail(string newEmail)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new ArgumentException("Email cannot be empty", nameof(newEmail));

        if (!IsValidEmail(newEmail))
            throw new ArgumentException("Invalid email format", nameof(newEmail));

        Email = newEmail.ToLowerInvariant();
    }

    /// <summary>
    /// Update user's password hash
    /// </summary>
    public void UpdatePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    /// <summary>
    /// Record user login
    /// </summary>
    public void RecordLogin()
    {
        if (Status != UserStatus.Active)
            throw new InvalidOperationException($"Cannot login - user account is {Status}");

        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Suspend the user account (can be reactivated)
    /// </summary>
    public void Suspend()
    {
        if (Status == UserStatus.Closed)
            throw new InvalidOperationException("Cannot suspend a closed account");

        Status = UserStatus.Suspended;
    }

    /// <summary>
    /// Reactivate a suspended account
    /// </summary>
    public void Reactivate()
    {
        if (Status == UserStatus.Closed)
            throw new InvalidOperationException("Cannot reactivate a closed account");

        Status = UserStatus.Active;
    }

    /// <summary>
    /// Permanently close the account
    /// </summary>
    public void Close()
    {
        Status = UserStatus.Closed;
    }

    /// <summary>
    /// Set the wallet for this user (called internally by Wallet)
    /// </summary>
    internal void SetWallet(Wallet wallet)
    {
        if (Wallet != null)
            throw new InvalidOperationException("User already has a wallet");

        Wallet = wallet;
    }

    public bool IsActive => Status == UserStatus.Active;
    public bool CanPlaceBets => Status == UserStatus.Active && Wallet != null;

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() => $"{Username} ({Email})";
}

/// <summary>
/// User account status
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Account is active and can place bets
    /// </summary>
    Active = 0,

    /// <summary>
    /// Account is temporarily suspended (can be reactivated)
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// Account is permanently closed
    /// </summary>
    Closed = 2
}
