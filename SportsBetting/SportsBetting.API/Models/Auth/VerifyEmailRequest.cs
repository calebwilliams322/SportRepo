namespace SportsBetting.API.Models.Auth;

/// <summary>
/// Request to verify email address
/// </summary>
public record VerifyEmailRequest
{
    public required string Token { get; init; }
}
