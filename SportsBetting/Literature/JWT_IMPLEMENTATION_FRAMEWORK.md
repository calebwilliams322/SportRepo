# JWT Authentication Implementation Framework

## Overview
This framework will guide you through completing the JWT authentication for the Sports Betting API.

**Current Status**: ✅ Domain layer and JWT service complete | ⏳ API endpoints and configuration needed

---

## Step 1: Add JWT Configuration to appsettings.json

**File**: `SportsBetting.API/appsettings.json`

### What to Add:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sportsbetting;Username=calebwilliams"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-characters-for-production-use",
    "Issuer": "SportsBettingAPI",
    "Audience": "SportsBettingClient",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### Security Notes:
- ⚠️ **NEVER commit the real secret to source control**
- Generate secret using: `openssl rand -base64 32`
- Use User Secrets for development: `dotnet user-secrets set "Jwt:Secret" "your-secret"`
- Use environment variables in production

---

## Step 2: Create Authentication DTOs

**Location**: `SportsBetting.API/Models/Auth/` (create this folder)

### 2.1 Create `LoginRequest.cs`:
```csharp
namespace SportsBetting.API.Models.Auth;

public record LoginRequest
{
    public required string UsernameOrEmail { get; init; }
    public required string Password { get; init; }
}
```

### 2.2 Create `RegisterRequest.cs`:
```csharp
using System.ComponentModel.DataAnnotations;

namespace SportsBetting.API.Models.Auth;

public record RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public required string Username { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public required string Password { get; init; }

    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; init; } = "USD";
}
```

### 2.3 Create `AuthResponse.cs`:
```csharp
namespace SportsBetting.API.Models.Auth;

public record AuthResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required UserInfo User { get; init; }
}

public record UserInfo
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Currency { get; init; }
}
```

### 2.4 Create `RefreshTokenRequest.cs`:
```csharp
namespace SportsBetting.API.Models.Auth;

public record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
```

---

## Step 3: Create Authentication Service

**Location**: `SportsBetting.API/Services/`

### 3.1 Create `IAuthService.cs`:
```csharp
using SportsBetting.API.Models.Auth;

namespace SportsBetting.API.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}
```

### 3.2 Create `AuthService.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using SportsBetting.API.Models.Auth;
using SportsBetting.Data;
using SportsBetting.Domain.Entities;

namespace SportsBetting.API.Services;

public class AuthService : IAuthService
{
    private readonly SportsBettingDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SportsBettingDbContext context,
        JwtService jwtService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        // Check if username or email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email, cancellationToken);

        if (existingUser != null)
        {
            throw new InvalidOperationException(
                existingUser.Username == request.Username
                    ? "Username already exists"
                    : "Email already exists");
        }

        // Create new user
        var user = User.CreateWithPassword(
            request.Username,
            request.Email,
            request.Password,
            request.Currency);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New user registered: {Username}", user.Username);

        // Generate tokens
        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Find user by username or email
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Username == request.UsernameOrEmail ||
                u.Email == request.UsernameOrEmail,
                cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Verify password
        if (!user.VerifyPassword(request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Check if user account is active
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException($"Account is {user.Status}");
        }

        // Record login
        user.RecordLogin();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in: {Username}", user.Username);

        // Generate tokens
        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        // Find the refresh token in database
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Validate the refresh token
        if (!storedToken.IsValid())
        {
            throw new UnauthorizedAccessException("Refresh token has expired or been revoked");
        }

        if (storedToken.User == null)
        {
            throw new InvalidOperationException("Refresh token has no associated user");
        }

        // Check if user is still active
        if (!storedToken.User.IsActive)
        {
            throw new UnauthorizedAccessException($"User account is {storedToken.User.Status}");
        }

        // Revoke the old refresh token (single-use refresh tokens for security)
        storedToken.Revoke();

        _logger.LogInformation("Refresh token used for user: {Username}", storedToken.User.Username);

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
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        // Generate access token
        var accessToken = _jwtService.GenerateAccessToken(user);

        // Generate refresh token
        var refreshTokenString = _jwtService.GenerateRefreshToken();
        var refreshToken = new RefreshToken(user, refreshTokenString, TimeSpan.FromDays(7));

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenString,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
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
```

---

## Step 4: Create Authentication Controller

**Location**: `SportsBetting.API/Controllers/AuthController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsBetting.API.Models.Auth;
using SportsBetting.API.Services;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return CreatedAtAction(nameof(Register), response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Login with username/email and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
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
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
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
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Logout by revoking refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken);
        return NoContent();
    }

    /// <summary>
    /// Get current user information (requires authentication)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var currency = User.FindFirst("currency")?.Value;

        if (userId == null || username == null || email == null)
        {
            return Unauthorized();
        }

        return Ok(new UserInfo
        {
            Id = Guid.Parse(userId),
            Username = username,
            Email = email,
            Currency = currency ?? "USD"
        });
    }
}
```

---

## Step 5: Configure JWT Authentication in Program.cs

**File**: `SportsBetting.API/Program.cs`

### Add the following configuration:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SportsBetting.API.Services;
using SportsBetting.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database
builder.Services.AddDbContext<SportsBettingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero // No tolerance for token expiration
    };
});

builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANT: Order matters!
app.UseAuthentication();  // Must come before UseAuthorization
app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## Step 6: Create Database Migration

### Run these commands:

```bash
# Navigate to the API project directory
cd SportsBetting.API

# Create migration for RefreshTokens
dotnet ef migrations add AddRefreshTokens --project ../SportsBetting.Data --startup-project .

# Apply migration to database
dotnet ef database update --project ../SportsBetting.Data --startup-project .
```

---

## Step 7: Protect Existing Endpoints

Add `[Authorize]` attribute to controllers/endpoints that require authentication:

### Example - `BetsController.cs`:
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]  // Require authentication for all endpoints
public class BetsController : ControllerBase
{
    // Your existing code...

    // You can also get the current user ID from the JWT:
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!.Value);
    }
}
```

---

## Step 8: Testing the Authentication Flow

### 8.1 Test Registration:
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test123456!",
    "currency": "USD"
  }'
```

### 8.2 Test Login:
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "usernameOrEmail": "testuser",
    "password": "Test123456!"
  }'
```

### 8.3 Test Authenticated Endpoint:
```bash
# Use the accessToken from login response
curl -X GET http://localhost:5000/api/auth/me \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```

### 8.4 Test Token Refresh:
```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN_HERE"
  }'
```

### 8.5 Test Logout:
```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN_HERE"
  }'
```

---

## Step 9: Security Best Practices

### ✅ Implemented:
- Password hashing with BCrypt
- JWT with short expiration (15 minutes)
- Refresh tokens stored in database (can be revoked)
- Single-use refresh tokens (revoked after use)

### 🔒 Additional Recommendations:
1. **Use HTTPS only** in production
2. **Store refresh tokens securely** on client (httpOnly cookies preferred over localStorage)
3. **Implement rate limiting** on login/register endpoints
4. **Add email verification** before account activation
5. **Implement password reset** functionality
6. **Add account lockout** after failed login attempts
7. **Log security events** (failed logins, token refreshes, etc.)
8. **Rotate JWT secrets** periodically
9. **Add CORS configuration** for your frontend
10. **Consider adding 2FA** for enhanced security

---

## Step 10: Optional Enhancements

### 10.1 Add User Secrets for Development:
```bash
dotnet user-secrets init --project SportsBetting.API
dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 32)" --project SportsBetting.API
```

### 10.2 Add Cleanup Job for Expired Tokens:
Create a background service to periodically remove expired refresh tokens from the database.

### 10.3 Add Swagger Authorization:
Already included in Step 5, allows testing authenticated endpoints directly in Swagger UI.

---

## Troubleshooting

### Common Issues:

1. **401 Unauthorized on protected endpoints**
   - Check that JWT is being sent in Authorization header
   - Verify JWT secret matches in appsettings.json and Program.cs
   - Check JWT hasn't expired

2. **Database migration fails**
   - Ensure PostgreSQL is running
   - Check connection string in appsettings.json
   - Verify you have permissions to create tables

3. **Token validation fails**
   - Check that Issuer and Audience match in JwtService and Program.cs
   - Verify clock is synchronized (ClockSkew = TimeSpan.Zero)

4. **User registration fails**
   - Check password meets minimum requirements (8 characters)
   - Verify username/email don't already exist
   - Check database connectivity

---

## Next Steps After Implementation

1. Add integration tests for authentication endpoints
2. Add unit tests for AuthService
3. Implement password reset functionality
4. Add email verification
5. Consider adding social login (Google, Facebook, etc.)
6. Implement role-based authorization if needed
7. Add audit logging for security events

---

## Summary

This framework provides everything needed to complete your JWT authentication implementation:

- ✅ Configuration files
- ✅ DTOs for requests/responses
- ✅ Complete AuthService with all authentication logic
- ✅ AuthController with all endpoints
- ✅ JWT middleware configuration
- ✅ Database migration instructions
- ✅ Testing examples
- ✅ Security best practices

Follow the steps in order, and you'll have a fully functional JWT authentication system!
