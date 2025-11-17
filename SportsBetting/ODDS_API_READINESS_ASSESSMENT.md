# Odds API Integration Readiness Assessment

**Date**: 2025-11-15
**Purpose**: Verify SportsBetting API is ready for The Odds API integration
**Status**: ⚠️ **MOSTLY READY** - Minor schema changes needed

---

## Executive Summary

The SportsBetting application has **excellent foundational support** for The Odds API integration:

✅ **Domain Model** - Well-structured entities for events, markets, and outcomes
✅ **Lifecycle Management** - Complete event state management (scheduled → in-progress → completed)
✅ **Dual-Mode Betting** - Full support for both sportsbook and exchange modes
✅ **Settlement System** - Comprehensive settlement with revenue tracking
✅ **Edge Cases** - Handles cancellations, suspensions, voids, and pushes

⚠️ **Missing Features** - Need to add 5 fields to support external API sync:
1. `Event.ExternalId` - Store Odds API event ID
2. `Market.ExternalId` - Store Odds API market ID
3. `Outcome.ExternalId` - Store Odds API outcome ID
4. `Event.LastSyncedAt` - Track last sync timestamp
5. `Outcome.LastOddsUpdate` - Track odds update timestamp

---

## ✅ What's Ready

### 1. Domain Entities - Event Lifecycle ✅

**File**: `SportsBetting.Domain/Entities/Event.cs`

**Verified Features**:
- ✅ `Event` entity with comprehensive properties
- ✅ `ScheduledStartTime` - Maps to Odds API `commence_time`
- ✅ `Status` enum - Supports all lifecycle states
  - `Scheduled` - Pre-game
  - `InProgress` - Live/in-play
  - `Completed` - Finished
  - `Cancelled` - Cancelled event
  - `Suspended` - Temporarily paused
- ✅ `FinalScore` - Stores completion results
- ✅ Lifecycle methods:
  - `Start()` - Begin event
  - `Complete(Score)` - Finish event with score
  - `Cancel()` - Cancel event
  - `Suspend()` - Pause event
  - `Resume()` - Resume from suspension

**Odds API Mapping**:
```csharp
// The Odds API Response → Event Entity
{
  "id": "abc123",                          // → Event.ExternalId (MISSING - need to add)
  "sport_key": "americanfootball_nfl",     // → Sport lookup
  "commence_time": "2025-11-15T18:00:00Z", // → Event.ScheduledStartTime ✅
  "home_team": "Kansas City Chiefs",        // → Event.HomeTeam ✅
  "away_team": "Las Vegas Raiders",         // → Event.AwayTeam ✅
  "bookmakers": [...]                       // → Markets/Outcomes ✅
}
```

---

### 2. Market Types ✅

**File**: `SportsBetting.Domain/Enums/MarketType.cs`

**Supported Market Types**:
- ✅ `Moneyline` - Maps to Odds API "h2h" market
- ✅ `Spread` - Maps to Odds API "spreads" market
- ✅ `Totals` - Maps to Odds API "totals" market
- ✅ `Prop` - Custom proposition bets
- ✅ `Futures` - Season-long bets

**Odds API Compatibility**: **100%** - All common market types supported

---

### 3. Outcome & Odds Management ✅

**File**: `SportsBetting.Domain/Entities/Outcome.cs`

**Verified Features**:
- ✅ `CurrentOdds` property - Stores decimal odds from API
- ✅ `Line` property (nullable decimal) - For spreads/totals
- ✅ `UpdateOdds(Odds newOdds)` method - **Critical for real-time updates**
- ✅ `IsWinner`, `IsVoid` flags - Settlement support

**Odds API Mapping**:
```csharp
// The Odds API Outcome → Outcome Entity
{
  "name": "Kansas City Chiefs",  // → Outcome.Name ✅
  "price": -125,                  // → Convert to decimal → Outcome.CurrentOdds ✅
  "point": -5.5                   // → Outcome.Line ✅ (for spreads)
}
```

**Odds Conversion** (American → Decimal):
```csharp
// Helper needed in SportsBettingListener
public static decimal AmericanToDecimal(int americanOdds)
{
    if (americanOdds > 0)
        return (americanOdds / 100m) + 1;
    else
        return (100m / Math.Abs(americanOdds)) + 1;
}

// Example: -125 → 1.80, +150 → 2.50
```

---

### 4. Dual-Mode Betting Support ✅

**File**: `SportsBetting.Domain/Entities/Market.cs`

**Verified Features**:
- ✅ `MarketMode` property (Sportsbook/Exchange/Hybrid)
- ✅ `ExchangeCommissionRate` property
- ✅ Mode switching capability

**For Odds API Integration**:
- **Sportsbook markets**: Use `Outcome.CurrentOdds` from The Odds API
- **Exchange markets**: Use user-defined odds (P2P matching), ignore API odds
- **Hybrid markets**: Both sportsbook odds (API) and exchange orders available

---

### 5. Settlement Workflow ✅

**File**: `SportsBetting.API/Controllers/SettlementController.cs`

**Verified Features**:
- ✅ Admin-only settlement endpoint (`/api/admin/settlement/event/{eventId}`)
- ✅ Integrated revenue tracking
  - `RecordSportsbookSettlement()` - House profit/loss
  - `RecordExchangeSettlement()` - Commission revenue
- ✅ Handles both bet modes in single transaction
- ✅ Automatic wallet updates
- ✅ User statistics tracking
- ✅ Commission tier progression

**Process Flow**:
1. The Odds API provides final scores → Listener updates Event
2. Admin calls settlement endpoint with final score
3. Event marked as Completed
4. Markets settled (winning outcomes marked)
5. All bets settled (sportsbook + exchange)
6. Revenue recorded
7. Wallets updated
8. User stats updated

---

### 6. Edge Case Handling ✅

**Event-Level**:
- ✅ `Event.Cancel()` - Cancelled games (refund all bets)
- ✅ `Event.Suspend()` / `Resume()` - Weather delays, technical issues
- ✅ Postponement handling (reschedule event)

**Market-Level**:
- ✅ `Market.SettleAsVoid()` - Void all outcomes (refund bets)
- ✅ `Market.SettleAsPush()` - Push/tie (refund bets)
- ✅ `Market.VoidOutcomes(outcomeIds)` - Void specific outcomes

**Real-World Scenarios**:
```
Scenario 1: Game Cancelled (Weather)
→ The Odds API marks event as cancelled
→ Listener calls Event.Cancel()
→ Settlement system refunds all bets

Scenario 2: Spread Push (Exact Line Hit)
→ Final score: Chiefs 24, Raiders 21 (spread was -3)
→ Chiefs covered by exactly 3 points
→ Market.SettleAsPush() → refund all bets

Scenario 3: Postponed Game
→ The Odds API updates commence_time
→ Listener updates Event.ScheduledStartTime
→ Event remains in Scheduled status
```

---

## ⚠️ What's Missing

### 1. External ID Tracking ❌

**Problem**: No way to link database entities to The Odds API identifiers

**Required Changes**:

#### Event Entity
```csharp
// ADD TO: SportsBetting.Domain/Entities/Event.cs

/// <summary>
/// External ID from The Odds API (e.g., "abc123def456")
/// Used to sync and update events from external source
/// </summary>
public string? ExternalId { get; private set; }

/// <summary>
/// Timestamp of last sync with The Odds API
/// </summary>
public DateTime? LastSyncedAt { get; private set; }

/// <summary>
/// Set external ID and sync timestamp (called by Listener)
/// </summary>
public void SetExternalId(string externalId)
{
    if (string.IsNullOrWhiteSpace(externalId))
        throw new ArgumentException("External ID cannot be empty", nameof(externalId));

    ExternalId = externalId;
    LastSyncedAt = DateTime.UtcNow;
}

/// <summary>
/// Update sync timestamp (called after each sync)
/// </summary>
public void UpdateSyncTimestamp()
{
    LastSyncedAt = DateTime.UtcNow;
}
```

#### Market Entity
```csharp
// ADD TO: SportsBetting.Domain/Entities/Market.cs

/// <summary>
/// External ID from The Odds API market key
/// (e.g., "h2h", "spreads", "totals")
/// </summary>
public string? ExternalId { get; private set; }

/// <summary>
/// Set external ID for API-created markets
/// </summary>
public void SetExternalId(string externalId)
{
    if (string.IsNullOrWhiteSpace(externalId))
        throw new ArgumentException("External ID cannot be empty", nameof(externalId));

    ExternalId = externalId;
}
```

#### Outcome Entity
```csharp
// ADD TO: SportsBetting.Domain/Entities/Outcome.cs

/// <summary>
/// External ID from The Odds API outcome name
/// (e.g., "Kansas City Chiefs", "Over 45.5")
/// </summary>
public string? ExternalId { get; private set; }

/// <summary>
/// Timestamp of last odds update from The Odds API
/// </summary>
public DateTime? LastOddsUpdate { get; private set; }

/// <summary>
/// Set external ID for API-created outcomes
/// </summary>
public void SetExternalId(string externalId)
{
    if (string.IsNullOrWhiteSpace(externalId))
        throw new ArgumentException("External ID cannot be empty", nameof(externalId));

    ExternalId = externalId;
}

/// <summary>
/// Update odds from The Odds API (enhanced version)
/// </summary>
public void UpdateOddsFromApi(Odds newOdds)
{
    CurrentOdds = newOdds;
    LastOddsUpdate = DateTime.UtcNow;
}
```

---

### 2. Database Migration ❌

**Required Migration**:

```bash
# Run from SportsBetting.Data project
dotnet ef migrations add AddOddsApiExternalIds
dotnet ef database update
```

**Migration Will Add**:
```sql
ALTER TABLE "Events"
  ADD COLUMN "ExternalId" VARCHAR(255) NULL,
  ADD COLUMN "LastSyncedAt" TIMESTAMP NULL;

ALTER TABLE "Markets"
  ADD COLUMN "ExternalId" VARCHAR(255) NULL;

ALTER TABLE "Outcomes"
  ADD COLUMN "ExternalId" VARCHAR(255) NULL,
  ADD COLUMN "LastOddsUpdate" TIMESTAMP NULL;

-- Add indexes for fast lookups
CREATE INDEX "IX_Events_ExternalId" ON "Events" ("ExternalId");
CREATE INDEX "IX_Events_LastSyncedAt" ON "Events" ("LastSyncedAt");
CREATE INDEX "IX_Outcomes_LastOddsUpdate" ON "Outcomes" ("LastOddsUpdate");
```

---

### 3. DbContext Configuration ❌

**File**: `SportsBetting.Data/SportsBettingDbContext.cs`

**Required Changes**:

```csharp
// ADD TO: OnModelCreating method

// Event configuration
modelBuilder.Entity<Event>(entity =>
{
    // ... existing configuration ...

    entity.Property(e => e.ExternalId)
        .HasMaxLength(255)
        .IsRequired(false);

    entity.Property(e => e.LastSyncedAt)
        .IsRequired(false);

    entity.HasIndex(e => e.ExternalId);
    entity.HasIndex(e => e.LastSyncedAt);
});

// Market configuration
modelBuilder.Entity<Market>(entity =>
{
    // ... existing configuration ...

    entity.Property(m => m.ExternalId)
        .HasMaxLength(255)
        .IsRequired(false);

    entity.HasIndex(m => m.ExternalId);
});

// Outcome configuration
modelBuilder.Entity<Outcome>(entity =>
{
    // ... existing configuration ...

    entity.Property(o => o.ExternalId)
        .HasMaxLength(255)
        .IsRequired(false);

    entity.Property(o => o.LastOddsUpdate)
        .IsRequired(false);

    entity.HasIndex(o => o.ExternalId);
    entity.HasIndex(o => o.LastOddsUpdate);
});
```

---

## 📊 Readiness Score

| Category | Status | Score | Notes |
|----------|--------|-------|-------|
| **Domain Model** | ✅ Ready | 100% | Event, Market, Outcome entities complete |
| **Lifecycle Management** | ✅ Ready | 100% | All state transitions supported |
| **Market Types** | ✅ Ready | 100% | Moneyline, Spread, Totals supported |
| **Odds Updates** | ✅ Ready | 100% | UpdateOdds() method exists |
| **Dual-Mode Betting** | ✅ Ready | 100% | Sportsbook + Exchange modes work |
| **Settlement** | ✅ Ready | 100% | Full settlement with revenue tracking |
| **Edge Cases** | ✅ Ready | 100% | Cancel, Suspend, Void, Push all supported |
| **External ID Tracking** | ❌ Missing | 0% | Need to add ExternalId fields |
| **Sync Timestamps** | ❌ Missing | 0% | Need LastSyncedAt, LastOddsUpdate |
| **Database Schema** | ❌ Missing | 0% | Need migration for new columns |

**Overall Readiness**: **70%** (7/10 categories ready)

---

## 🚀 Implementation Checklist

Before starting The Odds API integration, complete these tasks:

### Phase 1: Schema Changes (2-3 hours)
- [ ] Add `ExternalId` and `LastSyncedAt` to `Event` entity
- [ ] Add `SetExternalId()` and `UpdateSyncTimestamp()` methods to `Event`
- [ ] Add `ExternalId` to `Market` entity
- [ ] Add `SetExternalId()` method to `Market`
- [ ] Add `ExternalId` and `LastOddsUpdate` to `Outcome` entity
- [ ] Add `SetExternalId()` and `UpdateOddsFromApi()` methods to `Outcome`
- [ ] Update `SportsBettingDbContext` with new property configurations
- [ ] Create and run migration: `AddOddsApiExternalIds`
- [ ] Test migration on dev database
- [ ] Verify indexes were created

### Phase 2: Validation Tests (1 hour)
- [ ] Unit test: Event.SetExternalId()
- [ ] Unit test: Outcome.UpdateOddsFromApi() with timestamp
- [ ] Integration test: Query events by ExternalId
- [ ] Integration test: Query outcomes updated in last N minutes
- [ ] Performance test: Bulk odds updates (simulate 100 games)

### Phase 3: SportsBettingListener Development
- [ ] Follow `ODDS_API_INTEGRATION_PLAN.md` in SportsBettingListener directory
- [ ] Implement OddsApiClient
- [ ] Implement sync service
- [ ] Test end-to-end flow

---

## 📝 Additional Tests to Run

### Test 1: Event Creation from API Data
**Purpose**: Verify events can be created from The Odds API response

**Pseudo-code**:
```csharp
[Fact]
public void CreateEvent_WithExternalId_Success()
{
    // Arrange
    var oddsApiEventId = "abc123def456";
    var commenceTime = DateTime.UtcNow.AddHours(24);

    // Act
    var evt = new Event(
        name: "Kansas City Chiefs vs Las Vegas Raiders",
        homeTeam: kansasCityChiefs,
        awayTeam: lasVegasRaiders,
        scheduledStartTime: commenceTime,
        leagueId: nflLeagueId
    );
    evt.SetExternalId(oddsApiEventId);

    // Assert
    Assert.Equal(oddsApiEventId, evt.ExternalId);
    Assert.NotNull(evt.LastSyncedAt);
    Assert.True(evt.LastSyncedAt >= DateTime.UtcNow.AddSeconds(-5));
}
```

### Test 2: Odds Update Tracking
**Purpose**: Verify odds updates are timestamped

**Pseudo-code**:
```csharp
[Fact]
public void UpdateOdds_TracksTimestamp()
{
    // Arrange
    var outcome = new Outcome("Chiefs Win", "Chiefs ML", new Odds(1.80m));
    outcome.SetExternalId("chiefs_ml");

    var beforeUpdate = DateTime.UtcNow;
    Thread.Sleep(10); // Ensure time passes

    // Act
    outcome.UpdateOddsFromApi(new Odds(1.75m)); // Odds moved

    // Assert
    Assert.Equal(1.75m, outcome.CurrentOdds.DecimalValue);
    Assert.NotNull(outcome.LastOddsUpdate);
    Assert.True(outcome.LastOddsUpdate > beforeUpdate);
}
```

### Test 3: Event Sync Query Performance
**Purpose**: Verify fast lookups by ExternalId and LastSyncedAt

**Pseudo-code**:
```csharp
[Fact]
public async Task QueryByExternalId_Fast()
{
    // Arrange
    var externalId = "odds_api_event_123";

    // Act
    var stopwatch = Stopwatch.StartNew();
    var evt = await _context.Events
        .FirstOrDefaultAsync(e => e.ExternalId == externalId);
    stopwatch.Stop();

    // Assert
    Assert.NotNull(evt);
    Assert.True(stopwatch.ElapsedMilliseconds < 100); // Sub-100ms with index
}

[Fact]
public async Task QueryStaleEvents_Fast()
{
    // Arrange
    var staleThreshold = DateTime.UtcNow.AddMinutes(-5);

    // Act
    var stopwatch = Stopwatch.StartNew();
    var staleEvents = await _context.Events
        .Where(e => e.Status == EventStatus.Scheduled &&
                   (e.LastSyncedAt == null || e.LastSyncedAt < staleThreshold))
        .ToListAsync();
    stopwatch.Stop();

    // Assert
    Assert.True(stopwatch.ElapsedMilliseconds < 200); // Sub-200ms with index
}
```

### Test 4: Bulk Odds Update Performance
**Purpose**: Simulate updating odds for 100 games

**Pseudo-code**:
```csharp
[Fact]
public async Task BulkOddsUpdate_Performance()
{
    // Arrange
    var events = await CreateTestEvents(100); // 100 games, 3 markets each, 2-3 outcomes each
    var totalOutcomes = events.SelectMany(e => e.Markets)
                              .SelectMany(m => m.Outcomes)
                              .Count();

    // Act
    var stopwatch = Stopwatch.StartNew();
    foreach (var evt in events)
    {
        foreach (var market in evt.Markets)
        {
            foreach (var outcome in market.Outcomes)
            {
                // Simulate odds update from API
                outcome.UpdateOddsFromApi(new Odds(RandomOdds()));
            }
        }
        evt.UpdateSyncTimestamp();
    }
    await _context.SaveChangesAsync();
    stopwatch.Stop();

    // Assert
    var msPerOutcome = (double)stopwatch.ElapsedMilliseconds / totalOutcomes;
    Assert.True(msPerOutcome < 5); // <5ms per outcome update
    _logger.LogInformation($"Updated {totalOutcomes} outcomes in {stopwatch.ElapsedMilliseconds}ms ({msPerOutcome:F2}ms per outcome)");
}
```

---

## 🔍 Key Queries for The Odds API Listener

The Listener service will need these queries frequently:

### Find event by Odds API ID
```csharp
var evt = await _context.Events
    .Include(e => e.Markets)
        .ThenInclude(m => m.Outcomes)
    .FirstOrDefaultAsync(e => e.ExternalId == oddsApiEventId);
```

### Find stale events (haven't synced in 5+ minutes)
```csharp
var staleThreshold = DateTime.UtcNow.AddMinutes(-5);
var staleEvents = await _context.Events
    .Where(e => e.Status == EventStatus.Scheduled &&
               (e.LastSyncedAt == null || e.LastSyncedAt < staleThreshold))
    .ToListAsync();
```

### Find outcomes updated recently (for testing)
```csharp
var recentlyUpdated = await _context.Events
    .Include(e => e.Markets)
        .ThenInclude(m => m.Outcomes)
    .Where(e => e.Markets.Any(m =>
        m.Outcomes.Any(o => o.LastOddsUpdate >= DateTime.UtcNow.AddMinutes(-1))))
    .ToListAsync();
```

---

## 💡 Recommendations

### 1. Add Schema Changes First
**Why**: The Listener service won't work without ExternalId fields. Complete these changes before starting Listener development.

**Estimated Time**: 2-3 hours

### 2. Create Helper Service for Odds Conversion
**File**: `SportsBetting.Domain/Services/OddsConverter.cs`

```csharp
public static class OddsConverter
{
    /// <summary>
    /// Convert American odds to decimal (used by The Odds API)
    /// </summary>
    public static decimal AmericanToDecimal(int americanOdds)
    {
        if (americanOdds > 0)
            return (americanOdds / 100m) + 1;
        else
            return (100m / Math.Abs(americanOdds)) + 1;
    }

    /// <summary>
    /// Convert decimal to American odds (for display)
    /// </summary>
    public static int DecimalToAmerican(decimal decimalOdds)
    {
        if (decimalOdds >= 2.0m)
            return (int)((decimalOdds - 1) * 100);
        else
            return (int)(-100 / (decimalOdds - 1));
    }
}
```

### 3. Add Idempotency to Event Creation
**Why**: The Listener will poll The Odds API every 5 minutes. Need to avoid duplicate events.

**Pattern**:
```csharp
// In Listener service
var existingEvent = await _context.Events
    .FirstOrDefaultAsync(e => e.ExternalId == oddsApiEvent.Id);

if (existingEvent == null)
{
    // Create new event
    var newEvent = CreateEventFromOddsApi(oddsApiEvent);
    _context.Events.Add(newEvent);
}
else
{
    // Update existing event (odds, times, etc.)
    UpdateEventFromOddsApi(existingEvent, oddsApiEvent);
}

await _context.SaveChangesAsync();
```

---

## ✅ Conclusion

**The SportsBetting application is in excellent shape** for The Odds API integration. The domain model is well-designed with:
- Complete lifecycle management
- Flexible market/outcome structure
- Dual-mode betting support
- Comprehensive settlement system
- Edge case handling

**Before proceeding**:
1. ✅ Add the 5 missing fields (ExternalId, sync timestamps)
2. ✅ Run database migration
3. ✅ Write validation tests
4. ✅ Start SportsBettingListener development

**Estimated Total Prep Time**: **3-4 hours**

**After prep**, you'll be ready to implement the full Odds API integration following the `ODDS_API_INTEGRATION_PLAN.md` document.

---

**Next Steps**:
1. Review this assessment
2. Approve schema changes
3. Begin Phase 1 implementation
4. Test thoroughly
5. Move to SportsBettingListener directory for integration work
