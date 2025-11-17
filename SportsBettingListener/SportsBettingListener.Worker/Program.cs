using Microsoft.EntityFrameworkCore;
using SportsBetting.Data;
using SportsBetting.Domain.Configuration;
using SportsBetting.Domain.Services;
using SportsBettingListener.OddsApi;
using SportsBettingListener.ScoreApi;
using SportsBettingListener.Sync;
using SportsBettingListener.Sync.Mappers;
using SportsBettingListener.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add DbContext (shared with SportsBetting.API)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

builder.Services.AddDbContext<SportsBettingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add HttpClient for OddsApiClient
builder.Services.AddHttpClient<IOddsApiClient, OddsApiClient>();

// Add HttpClient for ScoreApiClient (ESPN)
builder.Services.AddHttpClient<IScoreApiClient, EspnApiClient>();

// Add Domain Configuration
builder.Services.AddSingleton(new CommissionConfiguration());

// Add Domain Services
builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<SettlementService>();

// Add Mappers and Sync Services (scoped because they use DbContext)
builder.Services.AddScoped<EventMapper>();
builder.Services.AddScoped<MarketMapper>();
builder.Services.AddScoped<EventSyncService>();
builder.Services.AddScoped<ScoreSyncService>();

// Add the background worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("SportsBettingListener Worker starting...");

// Mask password in connection string for logging
var maskedConnectionString = connectionString;
var passwordPart = connectionString.Split(";").FirstOrDefault(p => p.Contains("Password", StringComparison.OrdinalIgnoreCase));
if (!string.IsNullOrEmpty(passwordPart))
{
    var password = passwordPart.Split("=").LastOrDefault();
    if (!string.IsNullOrEmpty(password))
    {
        maskedConnectionString = connectionString.Replace(password, "***");
    }
}
logger.LogInformation("Database: {ConnectionString}", maskedConnectionString);

host.Run();
