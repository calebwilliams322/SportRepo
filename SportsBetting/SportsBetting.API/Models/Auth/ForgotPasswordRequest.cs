using System.ComponentModel.DataAnnotations;

namespace SportsBetting.API.Models.Auth;

/// <summary>
/// Request to initiate password reset
/// </summary>
public record ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }
}
