using System.ComponentModel.DataAnnotations;

namespace SportsBetting.API.Models.Auth;

/// <summary>
/// Request model for user registration
/// </summary>
public record RegisterRequest
{
    /// <summary>
    /// Desired username (3-50 characters)
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public required string Username { get; init; }

    /// <summary>
    /// Email address
    /// </summary>
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// Password (minimum 8 characters)
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }

    /// <summary>
    /// Preferred currency (3-letter ISO code, defaults to USD)
    /// </summary>
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "USD";
}
