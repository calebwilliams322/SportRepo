using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SportsBetting.API.Models.Auth;
using SportsBetting.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SportsBetting.API.Controllers;

/// <summary>
/// Authentication and user management endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Authentication response with tokens</returns>
    /// <response code="201">User registered successfully</response>
    /// <response code="400">Invalid request or username/email already exists</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            _logger.LogInformation("User registered via API: {Username}", request.Username);
            return CreatedAtAction(nameof(GetCurrentUser), response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Registration failed - invalid argument: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Login with username/email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with tokens</returns>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid credentials or account not active</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New authentication response with fresh tokens</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Token refresh error: {Message}", ex.Message);
            return Unauthorized(new { message = "Invalid refresh token" });
        }
    }

    /// <summary>
    /// Logout by revoking refresh token
    /// </summary>
    /// <param name="request">Refresh token to revoke</param>
    /// <returns>No content</returns>
    /// <response code="204">Logout successful</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken);
        _logger.LogInformation("User logged out: {UserId}", GetCurrentUserId());
        return NoContent();
    }

    /// <summary>
    /// Verify email address with token
    /// </summary>
    /// <param name="request">Email verification request</param>
    /// <returns>Success message</returns>
    /// <response code="200">Email verified successfully</response>
    /// <response code="400">Invalid or expired token</response>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            await _authService.VerifyEmailAsync(request.Token);
            return Ok(new { message = "Email verified successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Email verification failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Resend email verification
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <returns>Success message</returns>
    /// <response code="200">Verification email resent</response>
    /// <response code="400">User not found or email already verified</response>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerification([FromQuery] string email)
    {
        try
        {
            var token = await _authService.ResendEmailVerificationAsync(email);
            return Ok(new { message = "Verification email sent", token });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Resend verification failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Initiate password reset
    /// </summary>
    /// <param name="request">Forgot password request</param>
    /// <returns>Success message</returns>
    /// <response code="200">Password reset email sent</response>
    /// <response code="400">User not found</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            var token = await _authService.ForgotPasswordAsync(request.Email);
            return Ok(new { message = "Password reset email sent", token });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Forgot password failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reset password with token
    /// </summary>
    /// <param name="request">Reset password request</param>
    /// <returns>Success message</returns>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">Invalid or expired token</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request.Token, request.NewPassword);
            return Ok(new { message = "Password reset successfully" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Password reset failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    /// <returns>User information</returns>
    /// <response code="200">User information retrieved</response>
    /// <response code="401">Not authenticated</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var username = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        var currency = User.FindFirst("currency")?.Value;

        if (userId == null || username == null || email == null)
        {
            _logger.LogWarning("GetCurrentUser called but claims are missing");
            return Unauthorized(new { message = "Invalid authentication token" });
        }

        return Ok(new UserInfo
        {
            Id = Guid.Parse(userId),
            Username = username,
            Email = email,
            Currency = currency ?? "USD"
        });
    }

    /// <summary>
    /// Helper method to get current user ID from JWT claims
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}
