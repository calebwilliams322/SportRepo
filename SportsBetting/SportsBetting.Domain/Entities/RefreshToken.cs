namespace SportsBetting.Domain.Entities;

/// <summary>
/// Represents a refresh token for JWT authentication
/// Stored in database to allow revocation (logout)
/// </summary>
public class RefreshToken
{
    public Guid Id { get; }
    public Guid UserId { get; private set; }
    public string Token { get; }
    public DateTime ExpiresAt { get; }
    public DateTime CreatedAt { get; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    /// <summary>
    /// Navigation property to user
    /// </summary>
    public User? User { get; private set; }

    // Private parameterless constructor for EF Core
    private RefreshToken()
    {
        Token = string.Empty;
    }

    public RefreshToken(User user, string token, TimeSpan validFor)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token cannot be empty", nameof(token));

        if (validFor <= TimeSpan.Zero)
            throw new ArgumentException("Token must have positive validity period", nameof(validFor));

        Id = Guid.NewGuid();
        UserId = user.Id;
        User = user;
        Token = token;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(validFor);
        IsRevoked = false;
    }

    /// <summary>
    /// Revoke this refresh token (for logout or security)
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked)
            throw new InvalidOperationException("Token is already revoked");

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if token is currently valid
    /// </summary>
    public bool IsValid()
    {
        return !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Check if token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public override string ToString() => $"RefreshToken for User {UserId} - Expires: {ExpiresAt:g}";
}
