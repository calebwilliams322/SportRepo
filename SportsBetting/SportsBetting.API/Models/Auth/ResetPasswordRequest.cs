using System.ComponentModel.DataAnnotations;

namespace SportsBetting.API.Models.Auth;

/// <summary>
/// Request to reset password with token
/// </summary>
public record ResetPasswordRequest
{
    [Required]
    public required string Token { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string NewPassword { get; init; }
}
