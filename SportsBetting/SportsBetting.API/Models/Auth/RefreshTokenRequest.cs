namespace SportsBetting.API.Models.Auth;

/// <summary>
/// Request model for refreshing access token
/// </summary>
public record RefreshTokenRequest
{
    /// <summary>
    /// The refresh token to exchange for a new access token
    /// </summary>
    public required string RefreshToken { get; init; }
}
