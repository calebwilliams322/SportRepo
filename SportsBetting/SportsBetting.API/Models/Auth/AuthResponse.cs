namespace SportsBetting.API.Models.Auth;

/// <summary>
/// Response model for successful authentication
/// </summary>
public record AuthResponse
{
    /// <summary>
    /// JWT access token (short-lived, used for API requests)
    /// </summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Refresh token (long-lived, used to get new access tokens)
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// When the access token expires
    /// </summary>
    public required DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Information about the authenticated user
    /// </summary>
    public required UserInfo User { get; init; }
}

/// <summary>
/// User information returned in authentication responses
/// </summary>
public record UserInfo
{
    /// <summary>
    /// User's unique identifier
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Username
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Email address
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// User's preferred currency
    /// </summary>
    public required string Currency { get; init; }
}
