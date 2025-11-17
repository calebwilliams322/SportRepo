using SportsBetting.Domain.Enums;
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

    /// <summary>
    /// User's role in the system (Customer, Support, Admin)
    /// </summary>
    public UserRole Role { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? LastLoginAt { get; private set; }

    public UserStatus Status { get; private set; }

    /// <summary>
    /// Whether the user's email has been verified
    /// </summary>
    public bool EmailVerified { get; private set; }

    /// <summary>
    /// Token for email verification
    /// </summary>
    public string? EmailVerificationToken { get; private set; }

    /// <summary>
    /// When the email verification token expires
    /// </summary>
    public DateTime? EmailVerificationTokenExpires { get; private set; }

    /// <summary>
    /// Token for password reset
    /// </summary>
    public string? PasswordResetToken { get; private set; }

    /// <summary>
    /// When the password reset token expires
    /// </summary>
    public DateTime? PasswordResetTokenExpires { get; private set; }

    /// <summary>
    /// Navigation property to user's wallet
    /// </summary>
    public Wallet? Wallet { get; private set; }

    /// <summary>
    /// User's current commission tier (updated periodically based on 30-day volume)
    /// </summary>
    public CommissionTier CommissionTier { get; private set; }

    /// <summary>
    /// When the commission tier was last updated
    /// </summary>
    public DateTime? CommissionTierLastUpdated { get; private set; }

    /// <summary>
    /// Navigation property to user statistics
    /// </summary>
    public UserStatistics? Statistics { get; private set; }

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
        Role = UserRole.Customer; // Default role is Customer
        CreatedAt = DateTime.UtcNow;
        Status = UserStatus.Active;
        CommissionTier = CommissionTier.Standard; // Start at standard tier
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
    /// Set a new password (hashes it automatically)
    /// </summary>
    public void SetPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("Password cannot be empty", nameof(plainPassword));

        if (plainPassword.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters", nameof(plainPassword));

        PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    /// <summary>
    /// Verify if a password matches the stored hash
    /// </summary>
    public bool VerifyPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, PasswordHash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create a new user with a plain password (will be hashed)
    /// </summary>
    public static User CreateWithPassword(
        string username,
        string email,
        string plainPassword,
        string currency = "USD")
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("Password cannot be empty", nameof(plainPassword));

        if (plainPassword.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters", nameof(plainPassword));

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        return new User(username, email, passwordHash, currency);
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
    /// Set the user's role (admin operation)
    /// </summary>
    public void SetRole(UserRole role)
    {
        Role = role;
    }

    /// <summary>
    /// Check if user is an admin
    /// </summary>
    public bool IsAdmin => Role == UserRole.Admin;

    /// <summary>
    /// Check if user is support or admin
    /// </summary>
    public bool IsSupportOrAdmin => Role == UserRole.Support || Role == UserRole.Admin;

    /// <summary>
    /// Set the wallet for this user (called internally by Wallet)
    /// </summary>
    internal void SetWallet(Wallet wallet)
    {
        if (Wallet != null)
            throw new InvalidOperationException("User already has a wallet");

        Wallet = wallet;
    }

    /// <summary>
    /// Update the user's commission tier based on trading volume
    /// </summary>
    public void UpdateCommissionTier(CommissionTier newTier)
    {
        CommissionTier = newTier;
        CommissionTierLastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Set the statistics for this user (called internally by UserStatistics)
    /// </summary>
    internal void SetStatistics(UserStatistics stats)
    {
        if (Statistics != null)
            throw new InvalidOperationException("User already has statistics");

        Statistics = stats;
    }

    public bool IsActive => Status == UserStatus.Active;
    public bool CanPlaceBets => Status == UserStatus.Active && Wallet != null;

    /// <summary>
    /// Generate a new email verification token
    /// </summary>
    public string GenerateEmailVerificationToken()
    {
        EmailVerificationToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24);
        EmailVerified = false;
        return EmailVerificationToken;
    }

    /// <summary>
    /// Verify email with the provided token
    /// </summary>
    public bool VerifyEmail(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (EmailVerificationToken == null || EmailVerificationTokenExpires == null)
            return false;

        if (DateTime.UtcNow > EmailVerificationTokenExpires)
            return false;

        if (EmailVerificationToken != token)
            return false;

        EmailVerified = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpires = null;
        return true;
    }

    /// <summary>
    /// Generate a password reset token
    /// </summary>
    public string GeneratePasswordResetToken()
    {
        PasswordResetToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1); // 1 hour expiration
        return PasswordResetToken;
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    public bool ResetPassword(string token, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (PasswordResetToken == null || PasswordResetTokenExpires == null)
            return false;

        if (DateTime.UtcNow > PasswordResetTokenExpires)
            return false;

        if (PasswordResetToken != token)
            return false;

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters", nameof(newPassword));

        // Set the new password
        SetPassword(newPassword);

        // Clear the reset token
        PasswordResetToken = null;
        PasswordResetTokenExpires = null;

        return true;
    }

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

/// <summary>
/// User role for authorization
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular user - can only access their own data
    /// </summary>
    Customer = 0,

    /// <summary>
    /// Support staff - can view user data (read-only)
    /// </summary>
    Support = 1,

    /// <summary>
    /// Administrator - full access to all data and operations
    /// </summary>
    Admin = 2
}
