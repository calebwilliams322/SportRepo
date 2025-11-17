# The Odds API Integration - Complete Implementation Plan

**Project:** SportsBettingListener (New Microservice)
**Purpose:** Fetch live sports data from The Odds API and sync to SportsBetting database
**Architecture:** Standalone .NET worker service that communicates with SportsBetting API/Database

## ✅ Prerequisites Status

**Database Schema Changes:** ✅ **COMPLETE** (2025-11-15)
- Migration `AddOddsApiExternalIds` applied successfully
- All required ExternalId and sync timestamp fields added
- See: `/SportsBetting/ODDS_API_SCHEMA_CHANGES_COMPLETE.md`

**Ready to proceed with:** Listener service implementation (Phase 1+)

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Data Flow](#data-flow)
4. [Database Changes](#database-changes)
5. [API Contracts](#api-contracts)
6. [Implementation Steps](#implementation-steps)
7. [Configuration](#configuration)
8. [Testing Strategy](#testing-strategy)
9. [Deployment](#deployment)

---

## 1. Architecture Overview

### Current Architecture
```
┌─────────────────────────────────────┐
│     SportsBetting API               │
│                                     │
│  - Manual event creation (Admin)   │
│  - Sportsbook betting              │
│  - Exchange betting                │
│  - Revenue tracking                │
│  - Settlement                      │
└─────────────────────────────────────┘
           │
           ▼
    ┌──────────────┐
    │  PostgreSQL  │
    └──────────────┘
```

### Target Architecture
```
                 ┌──────────────────────┐
                 │   The Odds API       │
                 │  (External Service)  │
                 └──────────────────────┘
                           │
                           │ HTTPS
                           ▼
         ┌─────────────────────────────────────┐
         │  SportsBettingListener              │
         │  (New Worker Service)               │
         │                                     │
         │  - Fetch live sports data           │
         │  - Map to domain entities           │
         │  - Sync to database                 │
         │  - Update odds continuously         │
         │  - Handle event lifecycle           │
         └─────────────────────────────────────┘
                           │
                           │ Direct DB Access OR REST API
                           ▼
                    ┌──────────────┐
                    │  PostgreSQL  │
                    └──────────────┘
                           ▲
                           │
         ┌─────────────────────────────────────┐
         │     SportsBetting API               │
         │                                     │
         │  - Read events/markets/outcomes     │
         │  - Sportsbook betting (uses API odds)│
         │  - Exchange betting (user odds)     │
         │  - Revenue tracking                 │
         │  - Settlement                       │
         └─────────────────────────────────────┘
```

### Integration Approach: **Shared Database**

**Why:**
- Simplest integration
- No API calls between services
- Real-time data access
- Transaction consistency

**Alternative (Future):** REST API between services for better decoupling

---

## 2. Project Structure

### New Directory: `/SportRepo/SportsBettingListener`

```
SportRepo/
├── SportsBetting/              # Existing API
│   ├── SportsBetting.API/
│   ├── SportsBetting.Domain/   # Shared with Listener
│   ├── SportsBetting.Data/     # Shared with Listener
│   └── ...
│
└── SportsBettingListener/      # NEW PROJECT
    ├── SportsBettingListener.sln
    │
    ├── SportsBettingListener.Worker/     # Main worker service
    │   ├── Program.cs
    │   ├── Worker.cs                      # Background service
    │   ├── appsettings.json
    │   └── SportsBettingListener.Worker.csproj
    │
    ├── SportsBettingListener.OddsApi/    # The Odds API client
    │   ├── IOddsApiClient.cs
    │   ├── OddsApiClient.cs
    │   ├── Models/
    │   │   ├── OddsApiEvent.cs
    │   │   ├── OddsApiBookmaker.cs
    │   │   ├── OddsApiMarket.cs
    │   │   └── OddsApiOutcome.cs
    │   └── SportsBettingListener.OddsApi.csproj
    │
    ├── SportsBettingListener.Sync/       # Mapping & sync logic
    │   ├── ISyncService.cs
    │   ├── EventSyncService.cs
    │   ├── MarketSyncService.cs
    │   ├── OddsSyncService.cs
    │   ├── Mappers/
    │   │   ├── EventMapper.cs
    │   │   ├── MarketMapper.cs
    │   │   └── OutcomeMapper.cs
    │   └── SportsBettingListener.Sync.csproj
    │
    └── SportsBettingListener.Tests/
        ├── OddsApiClientTests.cs
        ├── MappingTests.cs
        └── SyncServiceTests.cs
```

### Project References

**SportsBettingListener.Worker** references:
- `SportsBettingListener.OddsApi`
- `SportsBettingListener.Sync`
- `../SportsBetting/SportsBetting.Domain` (shared)
- `../SportsBetting/SportsBetting.Data` (shared)

**Why share Domain and Data:**
- Use same entity models (Event, Market, Outcome, etc.)
- Use same DbContext
- Ensure data consistency
- No duplication

---

## 3. Data Flow

### 3.1 Event Lifecycle

```
┌────────────────────────────────────────────────────────────────┐
│  The Odds API → SportsBettingListener → Database → API        │
└────────────────────────────────────────────────────────────────┘

1. DISCOVERY
   The Odds API: "New event: Chiefs vs Raiders"
   Listener: Fetch event data
   Listener: Map to Event entity
   Listener: Create Sport, League, Teams if needed
   Listener: Insert Event into DB

2. MARKET CREATION
   The Odds API: Provides markets (Moneyline, Spread, Totals)
   Listener: Create Market entities
   Listener: Create Outcome entities
   Listener: Insert into DB

3. ODDS UPDATES (Continuous)
   The Odds API: Odds change (1.45 → 1.50)
   Listener: Update Outcome.CurrentOdds
   Listener: Update Market.LastUpdated

4. EVENT START
   The Odds API: Event status = "live"
   Listener: Update Event.Status = InProgress

5. EVENT COMPLETION
   The Odds API: Event no longer in upcoming/live
   Listener: Update Event.Status = Completed
   Admin (SportsBetting API): Provide final score, settle bets

6. CLEANUP
   Listener: Archive old events (optional)
```

### 3.2 Detailed Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                       The Odds API                              │
│  GET /v4/sports/americanfootball_nfl/odds                      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ JSON Response
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  OddsApiClient.FetchEventsAsync()               │
│  - HTTP GET with API key                                       │
│  - Deserialize to OddsApiEvent[]                               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                EventMapper.MapToEvent(oddsApiEvent)             │
│  - Map teams → Teams                                           │
│  - Map commence_time → ScheduledStartTime                      │
│  - Map sport_key → Sport                                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              EventSyncService.SyncEventAsync(event)             │
│  1. Check if event exists (by ExternalId)                      │
│  2. If new: Insert Event, Markets, Outcomes                    │
│  3. If existing: Update odds, status                           │
│  4. SaveChangesAsync()                                         │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   PostgreSQL     │
                    │   Events table   │
                    │   Markets table  │
                    │   Outcomes table │
                    └──────────────────┘
                              │
                              │ Reads
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│               SportsBetting API - MarketController              │
│  GET /api/markets → Returns synced events with current odds    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
                        ┌──────────┐
                        │  Users   │
                        │  Place   │
                        │  Bets    │
                        └──────────┘
```

---

## 4. Database Changes

> ✅ **STATUS: COMPLETE** (2025-11-15)
>
> Migration `20251115205200_AddOddsApiExternalIds` has been created and applied.
> All ExternalId and sync timestamp fields have been added to the database.
>
> See implementation details: `/SportsBetting/ODDS_API_SCHEMA_CHANGES_COMPLETE.md`

### 4.1 Implemented Schema Changes

**Events table:** ✅ COMPLETE
```sql
ALTER TABLE "Events" ADD COLUMN "ExternalId" VARCHAR(255);
ALTER TABLE "Events" ADD COLUMN "ExternalSource" VARCHAR(50) DEFAULT 'TheOddsAPI';
ALTER TABLE "Events" ADD COLUMN "LastSyncedAt" TIMESTAMP;

CREATE INDEX IX_Events_ExternalId ON "Events"("ExternalId");
```

**Markets table:** ✅ COMPLETE
```sql
ALTER TABLE "Markets" ADD COLUMN "ExternalId" VARCHAR(255);  -- ✅ ADDED
-- ALTER TABLE "Markets" ADD COLUMN "ExternalMarketKey" VARCHAR(100); -- Optional, not added yet
-- ALTER TABLE "Markets" ADD COLUMN "LastSyncedAt" TIMESTAMP; -- Optional, not added yet

CREATE INDEX IX_Markets_ExternalId ON "Markets"("ExternalId");  -- ✅ ADDED
```

**Outcomes table:** ✅ COMPLETE
```sql
ALTER TABLE "Outcomes" ADD COLUMN "ExternalId" VARCHAR(255);  -- ✅ ADDED
-- ALTER TABLE "Outcomes" ADD COLUMN "CurrentOdds" DECIMAL(10, 4); -- Already exists as complex property
ALTER TABLE "Outcomes" ADD COLUMN "LastOddsUpdate" TIMESTAMP;  -- ✅ ADDED

CREATE INDEX IX_Outcomes_ExternalId ON "Outcomes"("ExternalId");  -- ✅ ADDED
CREATE INDEX IX_Outcomes_LastOddsUpdate ON "Outcomes"("LastOddsUpdate");  -- ✅ ADDED
```

**Sports/Leagues/Teams:**
```sql
-- These might already exist, but ensure they have:
ALTER TABLE "Sports" ADD COLUMN "ExternalKey" VARCHAR(100); -- 'americanfootball_nfl'
ALTER TABLE "Leagues" ADD COLUMN "ExternalKey" VARCHAR(100);
ALTER TABLE "Teams" ADD COLUMN "ExternalName" VARCHAR(255); -- Exact name from API
```

### 4.2 Migration Name

**File:** `SportsBetting.Data/Migrations/YYYYMMDDHHMMSS_AddOddsApiIntegration.cs`

```csharp
public partial class AddOddsApiIntegration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ExternalId",
            table: "Events",
            type: "character varying(255)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExternalSource",
            table: "Events",
            type: "character varying(50)",
            nullable: false,
            defaultValue: "TheOddsAPI");

        // ... (add all columns as shown above)

        migrationBuilder.CreateIndex(
            name: "IX_Events_ExternalId",
            table: "Events",
            column: "ExternalId");
    }
}
```

---

## 5. API Contracts

### 5.1 The Odds API Response Structure

**Endpoint:** `GET https://api.the-odds-api.com/v4/sports/{sport}/odds`

**Query Parameters:**
- `apiKey` (required)
- `regions` = "us" (for US bookmakers)
- `markets` = "h2h,spreads,totals"
- `oddsFormat` = "decimal"

**Response:**
```json
[
  {
    "id": "abc123def456",
    "sport_key": "americanfootball_nfl",
    "sport_title": "NFL",
    "commence_time": "2025-11-20T18:00:00Z",
    "home_team": "Kansas City Chiefs",
    "away_team": "Las Vegas Raiders",
    "bookmakers": [
      {
        "key": "draftkings",
        "title": "DraftKings",
        "last_update": "2025-11-15T12:30:00Z",
        "markets": [
          {
            "key": "h2h",
            "last_update": "2025-11-15T12:30:00Z",
            "outcomes": [
              {
                "name": "Kansas City Chiefs",
                "price": 1.45
              },
              {
                "name": "Las Vegas Raiders",
                "price": 2.90
              }
            ]
          },
          {
            "key": "spreads",
            "last_update": "2025-11-15T12:30:00Z",
            "outcomes": [
              {
                "name": "Kansas City Chiefs",
                "price": 1.91,
                "point": -7.5
              },
              {
                "name": "Las Vegas Raiders",
                "price": 1.91,
                "point": 7.5
              }
            ]
          },
          {
            "key": "totals",
            "last_update": "2025-11-15T12:30:00Z",
            "outcomes": [
              {
                "name": "Over",
                "price": 1.87,
                "point": 48.5
              },
              {
                "name": "Under",
                "price": 1.95,
                "point": 48.5
              }
            ]
          }
        ]
      }
    ]
  }
]
```

### 5.2 Mapping to Domain Entities

**OddsApiEvent → Event:**
```
id → ExternalId
sport_key → Sport (lookup/create)
commence_time → ScheduledStartTime
home_team → HomeTeam (lookup/create)
away_team → AwayTeam (lookup/create)
ExternalSource = "TheOddsAPI"
Status = Scheduled (initially)
```

**OddsApiMarket → Market:**
```
key (h2h/spreads/totals) → ExternalMarketKey
Markets created:
  - "Moneyline" (for h2h)
  - "Point Spread" (for spreads)
  - "Total Points" (for totals)
```

**OddsApiOutcome → Outcome:**
```
name → Description
price → CurrentOdds
point → Handicap (for spreads/totals)
```

### 5.3 Odds Aggregation Strategy

**Problem:** The Odds API returns multiple bookmakers with different odds.

**Solution Options:**

**Option 1: Pick Best Bookmaker (Recommended)**
```csharp
var bestBookmaker = oddsApiEvent.Bookmakers
    .FirstOrDefault(b => b.Key == "draftkings")
    ?? oddsApiEvent.Bookmakers.FirstOrDefault();
```

**Option 2: Average Odds**
```csharp
var avgOdds = oddsApiEvent.Bookmakers
    .SelectMany(b => b.Markets)
    .Where(m => m.Key == "h2h")
    .SelectMany(m => m.Outcomes)
    .GroupBy(o => o.Name)
    .Select(g => new { Name = g.Key, AvgPrice = g.Average(o => o.Price) });
```

**Option 3: Store All Bookmakers (Complex)**
- Create BookmakerOdds table
- Let users choose bookmaker
- More data, more complexity

**Recommended:** Option 1 (DraftKings as primary source)

---

## 6. Implementation Steps

### Phase 1: Project Setup (Day 1)

**Step 1.1: Create Solution**
```bash
cd /Users/calebwilliams/SportRepo
mkdir SportsBettingListener
cd SportsBettingListener

# Create solution
dotnet new sln -n SportsBettingListener

# Create projects
dotnet new worker -n SportsBettingListener.Worker
dotnet new classlib -n SportsBettingListener.OddsApi
dotnet new classlib -n SportsBettingListener.Sync
dotnet new xunit -n SportsBettingListener.Tests

# Add to solution
dotnet sln add SportsBettingListener.Worker
dotnet sln add SportsBettingListener.OddsApi
dotnet sln add SportsBettingListener.Sync
dotnet sln add SportsBettingListener.Tests
```

**Step 1.2: Add Project References**
```bash
# Worker references everything
cd SportsBettingListener.Worker
dotnet add reference ../SportsBettingListener.OddsApi
dotnet add reference ../SportsBettingListener.Sync
dotnet add reference ../../SportsBetting/SportsBetting.Domain
dotnet add reference ../../SportsBetting/SportsBetting.Data

# Sync references OddsApi and Domain
cd ../SportsBettingListener.Sync
dotnet add reference ../SportsBettingListener.OddsApi
dotnet add reference ../../SportsBetting/SportsBetting.Domain
dotnet add reference ../../SportsBetting/SportsBetting.Data

# Tests reference everything
cd ../SportsBettingListener.Tests
dotnet add reference ../SportsBettingListener.Worker
dotnet add reference ../SportsBettingListener.OddsApi
dotnet add reference ../SportsBettingListener.Sync
```

**Step 1.3: Add NuGet Packages**
```bash
# Worker
cd SportsBettingListener.Worker
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.Http
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# OddsApi
cd ../SportsBettingListener.OddsApi
dotnet add package Microsoft.Extensions.Http
dotnet add package System.Text.Json

# Sync
cd ../SportsBettingListener.Sync
dotnet add package Microsoft.EntityFrameworkCore

# Tests
cd ../SportsBettingListener.Tests
dotnet add package Moq
dotnet add package FluentAssertions
```

### Phase 2: Database Migration (Day 1)

**Step 2.1: Create Migration**
```bash
cd ../../SportsBetting/SportsBetting.Data
dotnet ef migrations add AddOddsApiIntegration --startup-project ../SportsBetting.API
```

**Step 2.2: Review Migration**
- Check that all columns are added
- Verify indexes are created
- Ensure nullable/default values are correct

**Step 2.3: Apply Migration**
```bash
dotnet ef database update --startup-project ../SportsBetting.API
```

### Phase 3: The Odds API Client (Day 2)

**Step 3.1: Create Models** (`SportsBettingListener.OddsApi/Models/`)

**OddsApiEvent.cs:**
```csharp
public class OddsApiEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("sport_key")]
    public string SportKey { get; set; } = "";

    [JsonPropertyName("sport_title")]
    public string SportTitle { get; set; } = "";

    [JsonPropertyName("commence_time")]
    public DateTime CommenceTime { get; set; }

    [JsonPropertyName("home_team")]
    public string HomeTeam { get; set; } = "";

    [JsonPropertyName("away_team")]
    public string AwayTeam { get; set; } = "";

    [JsonPropertyName("bookmakers")]
    public List<OddsApiBookmaker> Bookmakers { get; set; } = new();
}
```

**OddsApiBookmaker.cs:**
```csharp
public class OddsApiBookmaker
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("last_update")]
    public DateTime LastUpdate { get; set; }

    [JsonPropertyName("markets")]
    public List<OddsApiMarket> Markets { get; set; } = new();
}
```

**OddsApiMarket.cs:**
```csharp
public class OddsApiMarket
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = ""; // h2h, spreads, totals

    [JsonPropertyName("last_update")]
    public DateTime LastUpdate { get; set; }

    [JsonPropertyName("outcomes")]
    public List<OddsApiOutcome> Outcomes { get; set; } = new();
}
```

**OddsApiOutcome.cs:**
```csharp
public class OddsApiOutcome
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("point")]
    public decimal? Point { get; set; } // For spreads/totals
}
```

**Step 3.2: Create Client Interface** (`IOddsApiClient.cs`)
```csharp
public interface IOddsApiClient
{
    Task<List<OddsApiEvent>> FetchEventsAsync(string sport, CancellationToken cancellationToken = default);
    Task<List<string>> GetAvailableSportsAsync(CancellationToken cancellationToken = default);
}
```

**Step 3.3: Implement Client** (`OddsApiClient.cs`)
```csharp
public class OddsApiClient : IOddsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<OddsApiClient> _logger;

    public OddsApiClient(HttpClient httpClient, IConfiguration config, ILogger<OddsApiClient> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["OddsApi:ApiKey"] ?? throw new Exception("API key missing");
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.the-odds-api.com/v4/");
    }

    public async Task<List<OddsApiEvent>> FetchEventsAsync(string sport, CancellationToken cancellationToken)
    {
        var url = $"sports/{sport}/odds?apiKey={_apiKey}&regions=us&markets=h2h,spreads,totals&oddsFormat=decimal";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var events = JsonSerializer.Deserialize<List<OddsApiEvent>>(content) ?? new();

            _logger.LogInformation("Fetched {Count} events for {Sport}", events.Count, sport);

            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch events for {Sport}", sport);
            throw;
        }
    }

    public async Task<List<string>> GetAvailableSportsAsync(CancellationToken cancellationToken)
    {
        // Implementation to fetch available sports
        // Returns: ["americanfootball_nfl", "basketball_nba", etc.]
    }
}
```

### Phase 4: Mapping Service (Day 2-3)

**Step 4.1: Event Mapper** (`SportsBettingListener.Sync/Mappers/EventMapper.cs`)

```csharp
public class EventMapper
{
    private readonly SportsBettingDbContext _context;

    public async Task<Event> MapToEventAsync(OddsApiEvent oddsEvent)
    {
        // 1. Get or create Sport
        var sport = await GetOrCreateSportAsync(oddsEvent.SportKey, oddsEvent.SportTitle);

        // 2. Get or create Teams
        var homeTeam = await GetOrCreateTeamAsync(oddsEvent.HomeTeam, sport);
        var awayTeam = await GetOrCreateTeamAsync(oddsEvent.AwayTeam, sport);

        // 3. Get or create League (assume default for now)
        var league = await GetOrCreateLeagueAsync(oddsEvent.SportKey, sport);

        // 4. Create Event
        var evt = new Event(
            homeTeam: homeTeam,
            awayTeam: awayTeam,
            league: league,
            scheduledStartTime: oddsEvent.CommenceTime.ToUniversalTime(),
            venue: "" // Not provided by API
        );

        evt.ExternalId = oddsEvent.Id;
        evt.ExternalSource = "TheOddsAPI";
        evt.LastSyncedAt = DateTime.UtcNow;

        return evt;
    }

    private async Task<Sport> GetOrCreateSportAsync(string sportKey, string sportTitle)
    {
        var sport = await _context.Sports
            .FirstOrDefaultAsync(s => s.ExternalKey == sportKey);

        if (sport == null)
        {
            sport = new Sport(sportTitle);
            sport.ExternalKey = sportKey;
            _context.Sports.Add(sport);
            await _context.SaveChangesAsync();
        }

        return sport;
    }

    // Similar methods for GetOrCreateTeamAsync, GetOrCreateLeagueAsync
}
```

**Step 4.2: Market Mapper** (`MarketMapper.cs`)

```csharp
public class MarketMapper
{
    public List<Market> MapMarketsForEvent(Event evt, OddsApiEvent oddsEvent)
    {
        var markets = new List<Market>();

        // Get primary bookmaker (DraftKings preferred)
        var bookmaker = oddsEvent.Bookmakers
            .FirstOrDefault(b => b.Key == "draftkings")
            ?? oddsEvent.Bookmakers.FirstOrDefault();

        if (bookmaker == null) return markets;

        // Map Moneyline (h2h)
        var h2hMarket = bookmaker.Markets.FirstOrDefault(m => m.Key == "h2h");
        if (h2hMarket != null)
        {
            var market = CreateMoneylineMarket(evt, h2hMarket);
            markets.Add(market);
        }

        // Map Spreads
        var spreadsMarket = bookmaker.Markets.FirstOrDefault(m => m.Key == "spreads");
        if (spreadsMarket != null)
        {
            var market = CreateSpreadMarket(evt, spreadsMarket);
            markets.Add(market);
        }

        // Map Totals
        var totalsMarket = bookmaker.Markets.FirstOrDefault(m => m.Key == "totals");
        if (totalsMarket != null)
        {
            var market = CreateTotalsMarket(evt, totalsMarket);
            markets.Add(market);
        }

        return markets;
    }

    private Market CreateMoneylineMarket(Event evt, OddsApiMarket oddsMarket)
    {
        var market = new Market(
            evt: evt,
            name: "Moneyline",
            marketType: MarketType.Moneyline
        );

        market.ExternalMarketKey = "h2h";
        market.LastSyncedAt = DateTime.UtcNow;

        // Create outcomes
        foreach (var oddsOutcome in oddsMarket.Outcomes)
        {
            var outcome = new Outcome(
                market: market,
                description: oddsOutcome.Name,
                displayOrder: 0
            );

            outcome.CurrentOdds = oddsOutcome.Price;
            outcome.LastOddsUpdate = DateTime.UtcNow;

            market.AddOutcome(outcome);
        }

        return market;
    }

    // Similar methods for CreateSpreadMarket, CreateTotalsMarket
}
```

### Phase 5: Sync Service (Day 3-4)

**Step 5.1: Event Sync Service** (`EventSyncService.cs`)

```csharp
public class EventSyncService
{
    private readonly SportsBettingDbContext _context;
    private readonly EventMapper _eventMapper;
    private readonly MarketMapper _marketMapper;
    private readonly ILogger<EventSyncService> _logger;

    public async Task SyncEventAsync(OddsApiEvent oddsEvent, CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Check if event exists
            var existingEvent = await _context.Events
                .Include(e => e.Markets)
                    .ThenInclude(m => m.Outcomes)
                .FirstOrDefaultAsync(e => e.ExternalId == oddsEvent.Id, cancellationToken);

            if (existingEvent == null)
            {
                // New event - create everything
                await CreateNewEventAsync(oddsEvent, cancellationToken);
            }
            else
            {
                // Existing event - update odds
                await UpdateEventOddsAsync(existingEvent, oddsEvent, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to sync event {EventId}", oddsEvent.Id);
            throw;
        }
    }

    private async Task CreateNewEventAsync(OddsApiEvent oddsEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new event: {Home} vs {Away}", oddsEvent.HomeTeam, oddsEvent.AwayTeam);

        // Map event
        var evt = await _eventMapper.MapToEventAsync(oddsEvent);
        _context.Events.Add(evt);
        await _context.SaveChangesAsync(cancellationToken);

        // Map markets
        var markets = _marketMapper.MapMarketsForEvent(evt, oddsEvent);
        foreach (var market in markets)
        {
            evt.AddMarket(market);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created event {EventId} with {MarketCount} markets", evt.Id, markets.Count);
    }

    private async Task UpdateEventOddsAsync(Event existingEvent, OddsApiEvent oddsEvent, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating odds for event {EventId}", existingEvent.Id);

        // Get primary bookmaker
        var bookmaker = oddsEvent.Bookmakers
            .FirstOrDefault(b => b.Key == "draftkings")
            ?? oddsEvent.Bookmakers.FirstOrDefault();

        if (bookmaker == null) return;

        // Update odds for each market
        foreach (var oddsMarket in bookmaker.Markets)
        {
            var market = existingEvent.Markets
                .FirstOrDefault(m => m.ExternalMarketKey == oddsMarket.Key);

            if (market == null) continue;

            foreach (var oddsOutcome in oddsMarket.Outcomes)
            {
                var outcome = market.Outcomes
                    .FirstOrDefault(o => o.Description == oddsOutcome.Name);

                if (outcome != null)
                {
                    outcome.CurrentOdds = oddsOutcome.Price;
                    outcome.LastOddsUpdate = DateTime.UtcNow;
                }
            }

            market.LastSyncedAt = DateTime.UtcNow;
        }

        existingEvent.LastSyncedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

### Phase 6: Background Worker (Day 4-5)

**Step 6.1: Worker Implementation** (`SportsBettingListener.Worker/Worker.cs`)

```csharp
public class Worker : BackgroundService
{
    private readonly IOddsApiClient _oddsApiClient;
    private readonly EventSyncService _syncService;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SportsBettingListener started at: {time}", DateTimeOffset.Now);

        // Get configuration
        var sports = _configuration.GetSection("OddsApi:Sports").Get<List<string>>()
            ?? new List<string> { "americanfootball_nfl", "basketball_nba" };

        var updateInterval = TimeSpan.FromMinutes(
            _configuration.GetValue<int>("OddsApi:UpdateIntervalMinutes", 5)
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllSportsAsync(sports, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sync cycle");
            }

            // Wait before next sync
            _logger.LogInformation("Next sync in {Minutes} minutes", updateInterval.TotalMinutes);
            await Task.Delay(updateInterval, stoppingToken);
        }
    }

    private async Task SyncAllSportsAsync(List<string> sports, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting sync cycle for {Count} sports", sports.Count);

        foreach (var sport in sports)
        {
            try
            {
                var events = await _oddsApiClient.FetchEventsAsync(sport, cancellationToken);

                _logger.LogInformation("Fetched {Count} events for {Sport}", events.Count, sport);

                foreach (var oddsEvent in events)
                {
                    await _syncService.SyncEventAsync(oddsEvent, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync sport {Sport}", sport);
            }
        }

        _logger.LogInformation("Sync cycle completed");
    }
}
```

**Step 6.2: Program.cs** (`SportsBettingListener.Worker/Program.cs`)

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Add DbContext (shared with SportsBetting.API)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("Connection string not configured");

builder.Services.AddDbContext<SportsBettingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add HttpClient for OddsApiClient
builder.Services.AddHttpClient<IOddsApiClient, OddsApiClient>();

// Add Sync Services
builder.Services.AddScoped<EventMapper>();
builder.Services.AddScoped<MarketMapper>();
builder.Services.AddScoped<EventSyncService>();

// Add Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

### Phase 7: Configuration (Day 5)

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sportsbetting;Username=calebwilliams"
  },
  "OddsApi": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "Sports": [
      "americanfootball_nfl",
      "basketball_nba",
      "icehockey_nhl",
      "baseball_mlb"
    ],
    "UpdateIntervalMinutes": 5,
    "PreferredBookmaker": "draftkings"
  }
}
```

---

## 7. Configuration

### 7.1 The Odds API Setup

1. **Get API Key:** https://the-odds-api.com/
2. **Free Tier:** 500 requests/month
3. **Pricing:** $10/month = 10,000 requests

### 7.2 Environment Variables

**For Development:**
```bash
export ODDS_API_KEY="your_api_key_here"
```

**For Production:**
```bash
# Azure App Service
az webapp config appsettings set --name sportsbetting-listener --settings ODDS_API__APIKEY=your_key
```

### 7.3 Database Connection

Both projects share the same database:
```
Host=localhost;Database=sportsbetting;Username=calebwilliams
```

---

## 8. Testing Strategy

### 8.1 Unit Tests

**OddsApiClientTests:**
```csharp
[Fact]
public async Task FetchEventsAsync_ShouldReturnEvents()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp.When("*/sports/americanfootball_nfl/odds*")
            .Respond("application/json", TestData.SampleOddsApiResponse);

    var client = new OddsApiClient(mockHttp.ToHttpClient(), config, logger);

    // Act
    var events = await client.FetchEventsAsync("americanfootball_nfl");

    // Assert
    events.Should().HaveCount(5);
    events.First().HomeTeam.Should().Be("Kansas City Chiefs");
}
```

**MappingTests:**
```csharp
[Fact]
public async Task EventMapper_ShouldMapCorrectly()
{
    // Arrange
    var oddsEvent = TestData.CreateSampleOddsApiEvent();
    var mapper = new EventMapper(context);

    // Act
    var evt = await mapper.MapToEventAsync(oddsEvent);

    // Assert
    evt.ExternalId.Should().Be(oddsEvent.Id);
    evt.HomeTeam.Name.Should().Be("Kansas City Chiefs");
    evt.ScheduledStartTime.Should().BeCloseTo(oddsEvent.CommenceTime);
}
```

### 8.2 Integration Tests

**End-to-End Sync Test:**
```csharp
[Fact]
public async Task SyncService_ShouldCreateNewEvent()
{
    // Arrange
    var oddsEvent = TestData.CreateSampleOddsApiEvent();
    var syncService = new EventSyncService(context, eventMapper, marketMapper, logger);

    // Act
    await syncService.SyncEventAsync(oddsEvent, CancellationToken.None);

    // Assert
    var evt = await context.Events
        .Include(e => e.Markets)
        .ThenInclude(m => m.Outcomes)
        .FirstOrDefaultAsync(e => e.ExternalId == oddsEvent.Id);

    evt.Should().NotBeNull();
    evt.Markets.Should().HaveCount(3); // Moneyline, Spread, Totals
}
```

### 8.3 Manual Testing

**Test Plan:**
1. Start SportsBettingListener
2. Verify events are created in database
3. Check SportsBetting API `/api/markets` returns new events
4. Place sportsbook bet using API odds
5. Verify odds update after 5 minutes
6. Test event lifecycle (scheduled → in progress → completed)

---

## 9. Deployment

### 9.1 Local Development

**Terminal 1: SportsBetting API**
```bash
cd /Users/calebwilliams/SportRepo/SportsBetting/SportsBetting.API
dotnet run
```

**Terminal 2: SportsBettingListener**
```bash
cd /Users/calebwilliams/SportRepo/SportsBettingListener/SportsBettingListener.Worker
dotnet run
```

### 9.2 Production Deployment

**Option 1: Docker Compose**
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: sportsbetting
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: ${DB_PASSWORD}

  api:
    build: ./SportsBetting/SportsBetting.API
    depends_on:
      - postgres
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=sportsbetting;Username=admin;Password=${DB_PASSWORD}"

  listener:
    build: ./SportsBettingListener/SportsBettingListener.Worker
    depends_on:
      - postgres
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=sportsbetting;Username=admin;Password=${DB_PASSWORD}"
      OddsApi__ApiKey: ${ODDS_API_KEY}
```

**Option 2: Azure App Service**
- Deploy API as Web App
- Deploy Listener as WebJob or Container Instance

**Option 3: Kubernetes**
- API: Deployment with HPA
- Listener: CronJob or Deployment (1 replica)

---

## 10. Data Flow Example

### Real-World Scenario: NFL Game

**T-0 (3 days before game):**
```
The Odds API: Chiefs vs Raiders scheduled for Sunday 6pm
Listener: Creates Event, 3 markets (Moneyline, Spread, Totals)
Database: Event created with Status=Scheduled
API: GET /api/markets returns new event
User: Sees "Chiefs vs Raiders" in app
```

**T-1 (2 days before game):**
```
The Odds API: Odds updated (Chiefs 1.45 → 1.40)
Listener: Updates Outcome.CurrentOdds
Database: Odds updated
User: Refreshes app, sees new odds
User: Places $100 sportsbook bet on Chiefs at 1.40
```

**T-2 (Game day, 5:55pm):**
```
The Odds API: Odds updated (last minute changes)
Listener: Updates Outcome.CurrentOdds
User: Can still place bets with latest odds
```

**T-3 (6:00pm, game starts):**
```
The Odds API: Event status = "live"
Listener: Updates Event.Status = InProgress
API: Closes betting (optional)
```

**T-4 (9:00pm, game ends):**
```
The Odds API: Event no longer in live feed
Listener: Updates Event.Status = Completed
Admin: Via SportsBetting API, provides final score (Chiefs 24, Raiders 17)
Admin: Settles all bets, revenue tracked automatically
```

---

## 11. Monitoring & Observability

### Key Metrics to Track

**Listener Metrics:**
- Events synced per cycle
- API requests to The Odds API (monitor quota)
- Failed sync attempts
- Odds update latency
- Database write performance

**Logging:**
```csharp
_logger.LogInformation("Sync completed: {EventCount} events, {Duration}ms", count, duration);
_logger.LogWarning("Odds API rate limit approaching: {RequestsRemaining}", remaining);
_logger.LogError(ex, "Failed to sync event {ExternalId}", externalId);
```

**Health Checks:**
- Database connectivity
- The Odds API availability
- Last successful sync timestamp

---

## 12. Cost Estimation

### The Odds API Costs

**Free Tier:**
- 500 requests/month
- Good for testing

**Paid Plans:**
- $10/month = 10,000 requests
- $50/month = 100,000 requests

**Example Usage:**
- 4 sports (NFL, NBA, NHL, MLB)
- Sync every 5 minutes
- 288 syncs/day × 4 sports = 1,152 requests/day
- Monthly: ~34,000 requests = $50/month

**Optimization:**
- Sync less frequently for events >24hrs away
- Sync every 5min for events <1hr away
- Pause syncing for off-season sports

---

## 13. Future Enhancements

### Phase 2 Features

1. **Live In-Play Betting**
   - The Odds API supports live scores
   - Update Event.Score in real-time
   - Allow betting during game

2. **Multiple Bookmaker Support**
   - Store odds from all bookmakers
   - Let users choose which odds to bet on
   - Show best available odds

3. **Historical Odds Tracking**
   - Store odds changes over time
   - Analyze line movements
   - Display odds charts

4. **Smart Odds Selection**
   - Average multiple bookmakers
   - Detect arbitrage opportunities
   - Optimize house edge

5. **Event Predictions**
   - ML models for outcome predictions
   - Adjust sportsbook odds based on betting patterns
   - Dynamic odds management

---

## 14. Quick Start Guide for Claude Code

**Context to provide in new session:**

```
You are working on SportsBettingListener - a .NET worker service that integrates
The Odds API with an existing sports betting platform.

Project Location: /Users/calebwilliams/SportRepo/SportsBettingListener

Goal: Fetch live sports events and odds from The Odds API and sync them to
the SportsBetting database.

Shared Resources:
- Domain entities: ../SportsBetting/SportsBetting.Domain
- Database context: ../SportsBetting/SportsBetting.Data
- Database: PostgreSQL (sportsbetting database)

Implementation Plan: /tmp/ODDS_API_INTEGRATION_PLAN.md

Start with Phase 1: Project Setup
```

---

## 15. Testing Checklist

Before considering integration complete, verify:

- [ ] SportsBettingListener project structure created
- [ ] Database migration applied (ExternalId, CurrentOdds, etc.)
- [ ] OddsApiClient can fetch events from The Odds API
- [ ] EventMapper correctly maps API data to Event entities
- [ ] MarketMapper creates Moneyline, Spread, Totals markets
- [ ] SyncService creates new events in database
- [ ] SyncService updates existing event odds
- [ ] Worker runs continuously and syncs every 5 minutes
- [ ] SportsBetting API can read synced events
- [ ] Users can place sportsbook bets using API odds
- [ ] Exchange betting still works on same events
- [ ] Odds update in real-time in UI
- [ ] Event lifecycle works (scheduled → in progress → completed)
- [ ] Logging and error handling in place
- [ ] Configuration via appsettings.json works
- [ ] Unit tests pass
- [ ] Integration tests pass
- [ ] Manual end-to-end test successful

---

## 16. Common Issues & Solutions

**Issue 1: "The Odds API rate limit exceeded"**
- Solution: Reduce update frequency, use caching, upgrade API plan

**Issue 2: "Duplicate events created"**
- Solution: Check ExternalId uniqueness, add unique constraint

**Issue 3: "Odds not updating"**
- Solution: Verify bookmaker mapping, check LastOddsUpdate timestamp

**Issue 4: "Worker crashes on startup"**
- Solution: Check database connection, verify migrations applied

**Issue 5: "Markets missing for some events"**
- Solution: Check bookmaker availability, log missing markets

---

## Summary

This integration plan provides a complete roadmap for building SportsBettingListener as a separate microservice that:

1. Fetches live sports data from The Odds API
2. Maps external data to your domain entities
3. Syncs events, markets, and odds to shared database
4. Runs continuously as a background worker
5. Integrates seamlessly with existing SportsBetting API

Users can then bet on automatically-populated events in both Sportsbook mode (using API odds) and Exchange mode (using custom P2P odds).

**Next Step:** Create the SportsBettingListener project following Phase 1 in a new Claude Code session.
