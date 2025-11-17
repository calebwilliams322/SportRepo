# Revenue Tracking System - Integration Guide

**Created:** November 15, 2025
**Status:** Ready for Integration

---

## Overview

The revenue tracking system tracks house revenue from two sources:
1. **Sportsbook (Book)** - Traditional house betting profit/loss
2. **Exchange (P2P)** - Commission from peer-to-peer bets

## Architecture

### Entities Created

**HouseRevenue** (`SportsBetting.Domain/Entities/HouseRevenue.cs`)
- Tracks revenue per time period (hourly/daily/monthly)
- Separates book revenue from exchange commission
- Calculates combined metrics automatically

**RevenueService** (`SportsBetting.API/Services/RevenueService.cs`)
- Records revenue when bets settle
- Creates/updates hourly revenue records
- Thread-safe for concurrent settlements

### API Endpoints

**RevenueController** (`SportsBetting.API/Controllers/RevenueController.cs`)
- `GET /api/admin/revenue/current-hour` - Current hour's revenue
- `GET /api/admin/revenue/today` - Today's aggregated revenue
- `GET /api/admin/revenue/range?startDate=X&endDate=Y` - Date range
- `GET /api/admin/revenue/hourly?date=X` - Hourly breakdown
- `GET /api/admin/revenue/comparison?startDate=X&endDate=Y` - Book vs Exchange

All endpoints require Admin role.

---

## Integration Steps

### Step 1: Register RevenueService in DI Container

**File:** `SportsBetting.API/Program.cs`

```csharp
// Add this with other service registrations
builder.Services.AddScoped<IRevenueService, RevenueService>();
```

### Step 2: Integrate into Exchange Bet Settlement

When exchange bets are settled, record the commission revenue.

**Example Location:** Wherever `SettlementService.SettleExchangeMatch()` is called

```csharp
public class ExchangeSettlementHandler
{
    private readonly IRevenueService _revenueService;
    private readonly SettlementService _settlementService;
    private readonly SportsBettingDbContext _context;

    public async Task SettleExchangeMatchAsync(
        BetMatch match,
        Outcome outcome,
        User backUser,
        User layUser)
    {
        // 1. Settle the match (existing code)
        var (winnerBet, netWinnings) = _settlementService.SettleExchangeMatch(
            match,
            outcome,
            backUser,
            layUser
        );

        // 2. Calculate commission (from match entity)
        var commission = match.BackBetWins
            ? match.BackBetCommission
            : match.LayBetCommission;

        // 3. Calculate winner payout
        var winnerPayout = match.MatchedStake + netWinnings.Amount;

        // 4. Record revenue (NEW!)
        _revenueService.RecordExchangeSettlement(
            match: match,
            commission: commission,
            winnerPayout: winnerPayout,
            settlementTime: DateTime.UtcNow
        );

        // 5. Save all changes
        await _context.SaveChangesAsync();
    }
}
```

### Step 3: Integrate into Sportsbook Bet Settlement

When traditional sportsbook bets are settled, record the profit/loss.

**Example Location:** Wherever sportsbook bets are marked Won/Lost

```csharp
public class SportsbookSettlementHandler
{
    private readonly IRevenueService _revenueService;
    private readonly SportsBettingDbContext _context;

    public async Task SettleSportsbookBetAsync(Bet bet)
    {
        // 1. Bet is already settled (status = Won or Lost)
        if (bet.BetMode != BetMode.Sportsbook)
            return;

        if (bet.Status != BetStatus.Won && bet.Status != BetStatus.Lost)
            return;

        // 2. Record revenue (NEW!)
        _revenueService.RecordSportsbookSettlement(
            bet: bet,
            settlementTime: DateTime.UtcNow
        );

        // 3. Save changes
        await _context.SaveChangesAsync();
    }
}
```

### Step 4: Batch Settlement Integration

If you settle multiple bets at once (e.g., when an event completes):

```csharp
public async Task SettleEventAsync(Event evt)
{
    // 1. Settle all bets for the event
    var bets = await _context.Bets
        .Where(b => b.Selections.Any(s => s.EventId == evt.Id))
        .ToListAsync();

    foreach (var bet in bets)
    {
        // Settle the bet (existing logic)
        _settlementService.SettleBet(bet, new[] { evt });

        // Record revenue
        if (bet.BetMode == BetMode.Sportsbook)
        {
            _revenueService.RecordSportsbookSettlement(bet);
        }
    }

    // 2. Settle all exchange matches for the event
    var matches = await _context.BetMatches
        .Include(m => m.BackBet).ThenInclude(eb => eb.Bet).ThenInclude(b => b.User)
        .Include(m => m.LayBet).ThenInclude(eb => eb.Bet).ThenInclude(b => b.User)
        .Where(m => /* matches for this event */)
        .ToListAsync();

    foreach (var match in matches)
    {
        var outcome = /* get winning outcome */;
        var backUser = match.BackBet.Bet.User;
        var layUser = match.LayBet.Bet.User;

        var (winnerBet, netWinnings) = _settlementService.SettleExchangeMatch(
            match, outcome, backUser, layUser
        );

        var commission = match.BackBetWins ? match.BackBetCommission : match.LayBetCommission;
        var payout = match.MatchedStake + netWinnings.Amount;

        _revenueService.RecordExchangeSettlement(match, commission, payout);
    }

    await _context.SaveChangesAsync();
}
```

---

## Database Migration

### Create Migration

```bash
cd SportsBetting.Data
dotnet ef migrations add AddHouseRevenue --startup-project ../SportsBetting.API
```

### Apply Migration

```bash
dotnet ef database update --startup-project ../SportsBetting.API
```

This creates the `HouseRevenue` table with:
- Primary key on `Id`
- Unique index on `(PeriodStart, PeriodEnd, PeriodType)`
- Index on `PeriodStart` for time-based queries

---

## Testing the System

### Manual Test: Record Some Revenue

```csharp
// Example: Simulating exchange revenue
var revenueService = services.GetRequiredService<IRevenueService>();

// Simulate $10 commission from a $500 match
var testMatch = new BetMatch(/* ... */);
testMatch.Settle(backBetWins: true, backCommission: 10m, layCommission: 0m);

revenueService.RecordExchangeSettlement(
    match: testMatch,
    commission: 10m,
    winnerPayout: 500m,
    settlementTime: DateTime.UtcNow
);

await context.SaveChangesAsync();
```

### Query Revenue via API

```bash
# Get current hour revenue
curl -H "Authorization: Bearer {admin-token}" \
  http://localhost:5192/api/admin/revenue/current-hour

# Get today's revenue
curl -H "Authorization: Bearer {admin-token}" \
  http://localhost:5192/api/admin/revenue/today

# Compare book vs exchange
curl -H "Authorization: Bearer {admin-token}" \
  "http://localhost:5192/api/admin/revenue/comparison?startDate=2025-11-01&endDate=2025-11-16"
```

---

## Revenue Metrics Explained

### Sportsbook Metrics

| Metric | Description | Calculation |
|--------|-------------|-------------|
| **Gross Revenue** | Total stakes from losing bets | Sum of all lost bet stakes |
| **Payouts** | Total paid to winning bets | Sum of all won bet payouts |
| **Net Revenue** | House profit | Gross Revenue - Payouts |
| **Hold %** | Profit margin | (Net Revenue / Volume) × 100 |

**Example:**
- 100 users bet $100 each = $10,000 volume
- 45 users win (house pays $11,000)
- 55 users lose (house keeps $5,500)
- **Gross Revenue:** $5,500 (from losers)
- **Payouts:** $11,000 (to winners)
- **Net Revenue:** -$5,500 (house lost money this period!)
- **Hold %:** -55% (bad day!)

### Exchange Metrics

| Metric | Description | Calculation |
|--------|-------------|-------------|
| **Commission Revenue** | Total commission earned | Sum of all commissions |
| **Volume** | Total matched stakes | Sum of all matched bet amounts |
| **Matches Count** | Number of settled matches | Count |
| **Effective Rate** | Average commission rate | (Commission / Volume) × 100 |

**Example:**
- 50 matches, $200 average stake = $10,000 volume
- Average commission tier: 1.2% (mostly Standard makers)
- **Commission Revenue:** $120 (1.2% of $10,000)
- **Volume:** $10,000
- **Matches Count:** 50
- **Effective Rate:** 1.2%

### Combined Metrics

| Metric | Description |
|--------|-------------|
| **Total Revenue** | Sportsbook Net + Exchange Commission |
| **Total Volume** | Sportsbook Volume + Exchange Volume |
| **Effective Margin** | (Total Revenue / Total Volume) × 100 |

---

## Example Revenue Report

```json
{
  "periodStart": "2025-11-15T00:00:00Z",
  "periodEnd": "2025-11-16T00:00:00Z",
  "periodType": "Daily",

  "sportsbookRevenue": 2500.00,
  "sportsbookVolume": 25000.00,
  "sportsbookBetsCount": 342,
  "sportsbookHoldPercentage": 10.0,

  "exchangeRevenue": 1200.00,
  "exchangeVolume": 100000.00,
  "exchangeMatchesCount": 156,
  "exchangeEffectiveRate": 1.2,

  "totalRevenue": 3700.00,
  "totalVolume": 125000.00,
  "effectiveMargin": 2.96
}
```

**Interpretation:**
- **Sportsbook:** Made $2,500 profit on $25k volume (10% hold)
- **Exchange:** Made $1,200 commission on $100k volume (1.2% rate)
- **Total:** $3,700 revenue on $125k total volume (2.96% margin)
- **Revenue Mix:** 67.6% from sportsbook, 32.4% from exchange

---

## Best Practices

### 1. Always Call RevenueService After Settlement

```csharp
// ✅ GOOD
_settlementService.SettleExchangeMatch(...);
_revenueService.RecordExchangeSettlement(...);
await _context.SaveChangesAsync();

// ❌ BAD - Revenue not tracked
_settlementService.SettleExchangeMatch(...);
await _context.SaveChangesAsync(); // Missing revenue call!
```

### 2. Use Transactions for Consistency

```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Settle bet
    _settlementService.SettleBet(bet, events);

    // Record revenue
    _revenueService.RecordSportsbookSettlement(bet);

    // Save all changes
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 3. Handle Time Zones Correctly

All times are stored in UTC. The RevenueService automatically handles hourly period boundaries in UTC.

### 4. Monitor for Missing Revenue

Periodically check that all settled bets have corresponding revenue records:

```sql
-- Find settled bets without revenue tracking
SELECT COUNT(*)
FROM "Bets"
WHERE "Status" IN ('Won', 'Lost')
  AND "BetMode" = 'Sportsbook'
  AND "SettledAt" IS NOT NULL
  -- Compare with HouseRevenue count
```

---

## Troubleshooting

### Revenue Not Showing Up

1. Check if SaveChanges() was called after RecordRevenue()
2. Verify the bet is in Won/Lost status
3. Check time zone - revenue records are in UTC
4. Verify Admin role for API access

### Duplicate Revenue Records

The system prevents duplicates via unique index on period boundaries. If you see errors about duplicate periods, the revenue was already recorded for that hour.

### Wrong Revenue Amounts

1. For Exchange: Verify commission calculation matches SettlementService
2. For Sportsbook: Check that ActualPayout is set correctly on Won bets
3. Review BetMatch.BackBetCommission and LayBetCommission values

---

## Summary

The revenue tracking system is now ready to use. To complete integration:

1. ✅ **Register RevenueService** in DI container
2. ✅ **Call RecordExchangeSettlement()** after each exchange match settlement
3. ✅ **Call RecordSportsbookSettlement()** after each sportsbook bet settlement
4. ✅ **Run database migration** to create HouseRevenue table
5. ✅ **Test API endpoints** with admin credentials
6. ✅ **Monitor revenue** via dashboard or API

The system will automatically:
- Create hourly revenue records
- Aggregate book vs exchange revenue separately
- Calculate profit margins and commission rates
- Provide queryable revenue history

---

*Integration Guide Version 1.0*
*Last Updated: November 15, 2025*
