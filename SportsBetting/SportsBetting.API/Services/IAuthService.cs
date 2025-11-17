using SportsBetting.API.Models.Auth;

namespace SportsBetting.API.Services;

/// <summary>
/// Service interface for authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with tokens</returns>
    /// <exception cref="InvalidOperationException">Thrown when username or email already exists</exception>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Login with username/email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication response with tokens</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid or account is not active</exception>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication response with fresh tokens</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when refresh token is invalid or expired</exception>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a refresh token (logout)
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify email address with token
    /// </summary>
    /// <param name="token">Email verification token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown when token is invalid or expired</exception>
    Task VerifyEmailAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend email verification
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New verification token</returns>
    /// <exception cref="InvalidOperationException">Thrown when user not found or already verified</exception>
    Task<string> ResendEmailVerificationAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiate password reset
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Password reset token</returns>
    /// <exception cref="InvalidOperationException">Thrown when user not found</exception>
    Task<string> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset password with token
    /// </summary>
    /// <param name="token">Password reset token</param>
    /// <param name="newPassword">New password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">Thrown when token is invalid or expired</exception>
    Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}
