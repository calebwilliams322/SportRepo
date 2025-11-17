using Microsoft.EntityFrameworkCore;
using SportsBetting.API.Models.Auth;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;

namespace SportsBetting.API.Services;

/// <summary>
/// Service for handling user authentication and authorization
/// </summary>
public class AuthService : IAuthService
{
    private readonly SportsBettingDbContext _context;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SportsBettingDbContext context,
        JwtService jwtService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to register user: {Username}", request.Username);

        // Check if username or email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            var conflict = existingUser.Username == request.Username ? "Username" : "Email";
            _logger.LogWarning("Registration failed: {Conflict} already exists", conflict);
            throw new InvalidOperationException($"{conflict} already exists");
        }

        // Create new user with hashed password
        var user = User.CreateWithPassword(
            request.Username,
            request.Email,
            request.Password,
            request.Currency);

        // Generate email verification token
        var verificationToken = user.GenerateEmailVerificationToken();

        _context.Users.Add(user);

        // Create user statistics for commission tracking
        var userStatistics = new UserStatistics(user);
        _context.UserStatistics.Add(userStatistics);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered successfully: {UserId} - {Username}", user.Id, user.Username);
        _logger.LogInformation("Email verification token for {Email}: {Token}", user.Email, verificationToken);
        _logger.LogInformation("Verification URL: http://localhost:5192/api/auth/verify-email?token={Token}", verificationToken);

        // Generate and return authentication tokens
        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Login attempt for: {UsernameOrEmail}", request.UsernameOrEmail);

        // Find user by username or email
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Username == request.UsernameOrEmail ||
                u.Email == request.UsernameOrEmail,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found - {UsernameOrEmail}", request.UsernameOrEmail);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Verify password
        if (!user.VerifyPassword(request.Password))
        {
            _logger.LogWarning("Login failed: Invalid password for user {Username}", user.Username);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Check if user account is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: Account {Status} for user {Username}", user.Status, user.Username);
            throw new UnauthorizedAccessException($"Account is {user.Status}");
        }

        // Record successful login
        user.RecordLogin();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in successfully: {UserId} - {Username}", user.Id, user.Username);

        // Generate and return authentication tokens
        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to refresh token");

        // Find the refresh token in database
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken == null)
        {
            _logger.LogWarning("Refresh failed: Token not found");
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Validate the refresh token
        if (!storedToken.IsValid())
        {
            _logger.LogWarning("Refresh failed: Token expired or revoked for user {UserId}", storedToken.UserId);
            throw new UnauthorizedAccessException("Refresh token has expired or been revoked");
        }

        if (storedToken.User == null)
        {
            _logger.LogError("Refresh failed: Token has no associated user - TokenId: {TokenId}", storedToken.Id);
            throw new InvalidOperationException("Refresh token has no associated user");
        }

        // Check if user is still active
        if (!storedToken.User.IsActive)
        {
            _logger.LogWarning("Refresh failed: User account {Status} - {UserId}", storedToken.User.Status, storedToken.User.Id);
            throw new UnauthorizedAccessException($"User account is {storedToken.User.Status}");
        }

        // Revoke the old refresh token (single-use refresh tokens for security)
        storedToken.Revoke();

        _logger.LogInformation("Refresh token used and revoked for user: {UserId} - {Username}",
            storedToken.User.Id, storedToken.User.Username);

        // Generate new tokens
        return await GenerateAuthResponseAsync(storedToken.User, cancellationToken);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken != null && !storedToken.IsRevoked)
        {
            storedToken.Revoke();
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Refresh token revoked for user: {UserId}", storedToken.UserId);
        }
        else if (storedToken == null)
        {
            _logger.LogWarning("Attempted to revoke non-existent token");
        }
        else
        {
            _logger.LogInformation("Attempted to revoke already-revoked token");
        }
    }

    public async Task VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to verify email with token");

        // Find user with this verification token
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Email verification failed: Invalid token");
            throw new InvalidOperationException("Invalid or expired verification token");
        }

        // Verify the email
        if (!user.VerifyEmail(token))
        {
            _logger.LogWarning("Email verification failed: Token expired or invalid for user {Email}", user.Email);
            throw new InvalidOperationException("Invalid or expired verification token");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email verified successfully for user: {UserId} - {Email}", user.Id, user.Email);
    }

    public async Task<string> ResendEmailVerificationAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to resend email verification for: {Email}", email);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Resend verification failed: User not found - {Email}", email);
            throw new InvalidOperationException("User not found");
        }

        if (user.EmailVerified)
        {
            _logger.LogWarning("Resend verification failed: Email already verified - {Email}", email);
            throw new InvalidOperationException("Email already verified");
        }

        // Generate new verification token
        var verificationToken = user.GenerateEmailVerificationToken();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New email verification token generated for {Email}: {Token}", user.Email, verificationToken);
        _logger.LogInformation("Verification URL: http://localhost:5192/api/auth/verify-email?token={Token}", verificationToken);

        return verificationToken;
    }

    public async Task<string> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset requested for: {Email}", email);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password reset failed: User not found - {Email}", email);
            throw new InvalidOperationException("User not found");
        }

        // Generate password reset token
        var resetToken = user.GeneratePasswordResetToken();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset token generated for {Email}: {Token}", user.Email, resetToken);
        _logger.LogInformation("Reset URL: http://localhost:5192/api/auth/reset-password (POST with token and newPassword)");

        return resetToken;
    }

    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to reset password with token");

        // Find user with this reset token
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password reset failed: Invalid token");
            throw new InvalidOperationException("Invalid or expired reset token");
        }

        // Reset the password
        if (!user.ResetPassword(token, newPassword))
        {
            _logger.LogWarning("Password reset failed: Token expired or invalid for user {Email}", user.Email);
            throw new InvalidOperationException("Invalid or expired reset token");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset successfully for user: {UserId} - {Email}", user.Id, user.Email);
    }

    /// <summary>
    /// Generate authentication response with access and refresh tokens
    /// </summary>
    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        // Generate access token (JWT)
        var accessToken = _jwtService.GenerateAccessToken(user);

        // Generate refresh token
        var refreshTokenString = _jwtService.GenerateRefreshToken();

        // Get refresh token expiration from configuration (default 7 days)
        var refreshTokenExpirationDays = _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);
        var refreshToken = new RefreshToken(user, refreshTokenString, TimeSpan.FromDays(refreshTokenExpirationDays));

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Get access token expiration from configuration (default 15 minutes)
        var accessTokenExpirationMinutes = _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes),
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Currency = user.Currency
            }
        };
    }
}
