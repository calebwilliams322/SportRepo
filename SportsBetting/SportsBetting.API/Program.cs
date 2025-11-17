using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SportsBetting.API.Hubs;
using SportsBetting.API.Services;
using SportsBetting.Data;
using SportsBetting.Domain.Configuration;
using SportsBetting.Domain.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add SignalR for WebSocket support
builder.Services.AddSignalR();

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=sportsbetting;Username=calebwilliams";

builder.Services.AddDbContext<SportsBettingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Register domain services
builder.Services.AddScoped<WalletService>();
builder.Services.AddScoped<SettlementService>();
builder.Services.AddScoped<IRevenueService, RevenueService>();

// Configure and register commission service
builder.Services.Configure<CommissionConfiguration>(
    builder.Configuration.GetSection("CommissionConfiguration"));
builder.Services.AddScoped<ICommissionService>(sp =>
{
    var config = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CommissionConfiguration>>().Value;
    return new CommissionService(config);
});

// Register matching strategy
// Options: FifoMatchingStrategy, ProRataMatchingStrategy, ProRataWithTopMatchingStrategy
// Default: ProRataWithTopMatchingStrategy (hybrid: 40% FIFO for top order, 60% pro-rata for rest)
builder.Services.AddScoped<IMatchingStrategy>(sp =>
{
    // Configuration options:
    // 1. Pure FIFO (original): new FifoMatchingStrategy()
    // 2. Pure Pro-Rata: new ProRataMatchingStrategy()
    // 3. Hybrid (recommended): new ProRataWithTopMatchingStrategy(topOrderCount, topAllocationPercent)

    return new ProRataWithTopMatchingStrategy(
        topOrderCount: 1,           // Give priority to first 1 order
        topAllocationPercent: 0.40m // 40% allocated FIFO, 60% pro-rata
    );
});

// Register exchange betting services
builder.Services.AddScoped<IBetMatchingService, BetMatchingService>();
builder.Services.AddScoped<IOddsValidationService, OddsValidationService>();
builder.Services.AddMemoryCache(); // For consensus odds caching

// Configure Feature Flags for hybrid betting system
builder.Services.Configure<FeatureFlags>(
    builder.Configuration.GetSection("FeatureFlags"));

// Register authentication services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false; // Preserve JWT claim names (sub, name, email) instead of mapping to .NET claim types

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

    // Configure JWT for SignalR (token passed via query string)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Admin-only policy
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // Support or Admin policy (read-only access to user data)
    options.AddPolicy("SupportOrAdmin", policy =>
        policy.RequireRole("Support", "Admin"));

    // Customer policy (just for completeness)
    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireRole("Customer"));
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Global rate limit - 1000 requests per minute per IP (increased for frontend with multiple API calls)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1000,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));

    // Strict rate limit for authentication endpoints - 5 requests per minute per IP
    options.AddFixedWindowLimiter("auth", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });

    // Custom rejection response
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "Too many requests. Please try again later.",
            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? retryAfter.ToString()
                : "60 seconds"
        }, cancellationToken);
    };
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SportsBetting API",
        Version = "v1",
        Description = "REST API for sports betting platform with JWT authentication",
        Contact = new OpenApiContact
        {
            Name = "SportsBetting Platform"
        }
    });

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your JWT token.\n\nExample: 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS (for frontend development and SignalR)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:8000", "https://localhost:7192")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SportsBetting API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

// Enable static files (for SignalR client example)
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Rate limiting must come before authentication/authorization
app.UseRateLimiter();

// IMPORTANT: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<OrderBookHub>("/hubs/orderbook");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("System");

app.Run();

// Make Program class accessible to integration tests
public partial class Program { }
