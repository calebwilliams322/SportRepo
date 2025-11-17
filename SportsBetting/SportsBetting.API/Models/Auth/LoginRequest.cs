namespace SportsBetting.API.Models.Auth;

/// <summary>
/// Request model for user login
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// Username or email address
    /// </summary>
    public required string UsernameOrEmail { get; init; }

    /// <summary>
    /// User's password
    /// </summary>
    public required string Password { get; init; }
}
