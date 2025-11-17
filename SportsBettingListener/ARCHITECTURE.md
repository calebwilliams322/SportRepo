# SportsBetting System Architecture

## System Components

```
┌──────────────────────────────────────────────────────────────┐
│                    EXTERNAL SERVICES                          │
├──────────────────────────────────────────────────────────────┤
│  The Odds API          │  ESPN API                            │
│  (Live Odds)           │  (Live Scores)                       │
│  DraftKings, etc.      │  Game Results                        │
└────────┬───────────────┴──────────┬───────────────────────────┘
         │                          │
         │ HTTP Requests            │ HTTP Requests
         │ (Every 5 min)            │ (Continuous)
         │                          │
┌────────▼──────────────────────────▼───────────────────────────┐
│              SPORTSBETTING WORKER SERVICE                     │
│          (SportsBettingListener.Worker)                       │
├───────────────────────────────────────────────────────────────┤
│  - OddsApiClient: Fetches odds from The Odds API              │
│  - EspnApiClient: Fetches scores from ESPN                    │
│  - EventSyncService: Creates/updates events in DB             │
│  - ScoreSyncService: Matches completed games                  │
│  - SettlementService: Auto-settles bets                       │
│                                                               │
│  Runs: Continuously in background                            │
│  Schedule: Updates every 5 minutes                           │
└────────┬──────────────────────────────────────────────────────┘
         │
         │ Writes to database
         │ (Events, Markets, Outcomes, OddsHistory)
         │
┌────────▼──────────────────────────────────────────────────────┐
│                   POSTGRESQL DATABASE                         │
│                    (sportsbetting)                            │
├───────────────────────────────────────────────────────────────┤
│  Tables:                                                      │
│  - Events: NFL/NBA games                                      │
│  - Markets: Moneyline, Spread, Totals                         │
│  - Outcomes: Team names with CurrentOddsDecimal               │
│  - OddsHistory: Historical odds for tracking movements        │
│  - Bets: User bet placements                                  │
│  - Wallets: User balances                                     │
│  - ExternalEventMappings: Links our events to ESPN/Odds API   │
└────────┬──────────────────────────────────────────────────────┘
         │
         │ Reads from database
         │ (API never calls external services)
         │
┌────────▼──────────────────────────────────────────────────────┐
│               SPORTSBETTING REST API                          │
│            (SportsBetting.API - Port 5192)                    │
├───────────────────────────────────────────────────────────────┤
│  Controllers:                                                 │
│  - EventsController: Browse events and odds (PUBLIC)          │
│  - BetsController: Place bets (AUTHENTICATED)                 │
│  - WalletsController: Check balance (AUTHENTICATED)           │
│  - AuthController: Register/login (PUBLIC)                    │
│                                                               │
│  Endpoints:                                                   │
│  - GET /api/events                                            │
│  - GET /api/events/markets/{id}                               │
│  - POST /api/bets/single                                      │
│  - POST /api/bets/parlay                                      │
│  - GET /api/wallets/user/{userId}                             │
│                                                               │
│  Runs: dotnet run (port 5192)                                │
└────────┬──────────────────────────────────────────────────────┘
         │
         │ HTTP/JSON responses
         │
┌────────▼──────────────────────────────────────────────────────┐
│                    FRONTEND / USERS                           │
│              (React, Mobile App, etc.)                        │
├───────────────────────────────────────────────────────────────┤
│  - Browse upcoming games and live odds                        │
│  - Place single and parlay bets                               │
│  - View bet history and wallet balance                        │
│  - See real-time odds updates (refreshes from API)            │
└───────────────────────────────────────────────────────────────┘
```

## Data Flow Example: User Places a Bet

### Step 1: Worker Populates Database (Background)
```
12:00 PM - Worker Cycle
├─ Fetch NFL games from The Odds API
├─ Create event: "Chiefs vs Broncos"
├─ Create market: "Moneyline"
├─ Create outcomes:
│  ├─ Chiefs @ 1.46 odds
│  └─ Broncos @ 2.80 odds
└─ Save to PostgreSQL
```

### Step 2: User Browses Events
```
User → GET /api/events?status=Scheduled
API → SELECT * FROM Events WHERE Status = 'Scheduled'
API → Returns JSON with all upcoming games
```

### Step 3: User Views Betting Market
```
User → GET /api/events/markets/{marketId}
API → SELECT * FROM Outcomes WHERE MarketId = {id}
API → Returns:
      {
        "outcomes": [
          { "name": "Chiefs", "currentOdds": 1.46 },
          { "name": "Broncos", "currentOdds": 2.80 }
        ]
      }
```

### Step 4: User Places Bet
```
User → POST /api/bets/single
       { "outcomeId": "chiefs-id", "stake": 100 }

API → BEGIN TRANSACTION
API → Deduct $100 from wallet
API → Create bet record (locked odds: 1.46)
API → COMMIT TRANSACTION
API → Return bet confirmation
```

### Step 5: Worker Updates Odds (Background)
```
12:05 PM - Worker Cycle
├─ Fetch updated odds from The Odds API
├─ Chiefs odds changed: 1.46 → 1.52
├─ UPDATE Outcomes SET CurrentOddsDecimal = 1.52
├─ INSERT INTO OddsHistory (odds=1.52, timestamp=12:05)
└─ User's bet is UNAFFECTED (locked at 1.46)
```

### Step 6: Game Completes (Background)
```
4:30 PM - Game Ends
├─ Worker fetches scores from ESPN
├─ Chiefs won 28-24
├─ Worker runs SettlementService
├─ Mark Chiefs outcome as winner
├─ Settle all bets:
│  ├─ User's bet: Won!
│  ├─ Payout: $146 (stake × odds)
│  └─ UPDATE Wallets: Add $146 to user's balance
└─ User sees updated balance next time they check
```

## Why This Architecture?

### Separation of Concerns
- **Worker**: Heavy lifting (API calls, data sync, settlement)
- **API**: Fast responses (just database reads)
- **Database**: Single source of truth

### Performance
- API responses are FAST (no external API calls)
- Worker handles slow external API calls in background
- Database caching keeps API snappy

### Scalability
- Can run multiple API instances (all read from same DB)
- Worker is single instance (prevents duplicate API calls)
- Database handles concurrent reads/writes

### Cost Efficiency
- Free tier Odds API: 500 requests total
- Worker makes ~76 full sync cycles with current setup
- API makes ZERO external requests (preserves quota)

## Running the System

### Development (Manual)

**Terminal 1 - Run API:**
```bash
cd /Users/calebwilliams/SportRepo/SportsBetting/SportsBetting.API
dotnet run
```
API available at: http://localhost:5192

**Terminal 2 - Run Worker:**
```bash
cd /Users/calebwilliams/SportRepo/SportsBettingListener/SportsBettingListener.Worker
dotnet run
```
Worker syncs odds every 5 minutes

### Development (Screen Sessions)

**Start both in screen sessions:**
```bash
# Start API in screen
screen -S api
cd /Users/calebwilliams/SportRepo/SportsBetting/SportsBetting.API
dotnet run
# Press Ctrl+A, then D to detach

# Start Worker in screen
screen -S worker
cd /Users/calebwilliams/SportRepo/SportsBettingListener/SportsBettingListener.Worker
dotnet run
# Press Ctrl+A, then D to detach
```

**Check on them later:**
```bash
screen -ls              # List all screens
screen -r api           # Reattach to API
screen -r worker        # Reattach to Worker
```

### Production (Background Services)

See `BACKGROUND_SERVICE_SETUP.md` for full setup instructions.

## Configuration

### Worker Configuration
**File:** `SportsBettingListener.Worker/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sportsbetting;Username=calebwilliams"
  },
  "OddsApi": {
    "ApiKey": "461eb31147971bb22b919d4d236342b4",
    "Sports": ["americanfootball_nfl", "basketball_nba"],
    "UpdateIntervalMinutes": 5,
    "PreferredBookmaker": "draftkings"
  },
  "ScoreApi": {
    "Provider": "ESPN",
    "EnableAutoSettlement": true,
    "DryRunMode": false
  }
}
```

### API Configuration
**File:** `SportsBetting.API/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sportsbetting;Username=calebwilliams"
  },
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "SportsBettingAPI",
    "Audience": "SportsBettingClient",
    "ExpirationHours": 24
  }
}
```

## Database Schema

Both API and Worker share the same database context (`SportsBettingDbContext`).

**Key Tables:**
- `Events`: Sporting events (NFL/NBA games)
- `Markets`: Betting markets (Moneyline, Spread, Totals)
- `Outcomes`: Individual betting options with odds
- `OddsHistory`: Historical odds tracking
- `Bets`: User bet placements
- `BetSelections`: Individual legs of bets
- `Wallets`: User account balances
- `ExternalEventMappings`: Maps our events to Odds API/ESPN IDs

## API Rate Limiting

**The Odds API Free Tier:**
- 500 requests total (not per month - TOTAL)
- Current usage: ~28 events × 2 sports = ~56 events
- Each sync cycle uses ~1-2 requests
- Estimated cycles available: ~76 syncs
- At 5-minute intervals: ~6.3 hours of runtime

**Recommendations:**
- For development: Run worker only when needed
- For production: Upgrade to paid tier ($10-50/month)
- Monitor usage: Check remaining requests regularly

## Troubleshooting

### Worker Not Updating Odds
1. Check worker is running: `ps aux | grep SportsBettingListener.Worker`
2. Check logs for errors
3. Verify API key is valid and has remaining requests
4. Check database connection string

### API Returns Stale Odds
1. Worker might not be running
2. Check last sync: `SELECT MAX(LastOddsUpdate) FROM Outcomes`
3. Manually run worker to sync latest odds

### Auto-Settlement Not Working
1. Verify `EnableAutoSettlement: true` in worker config
2. Check events have `ExternalEventMapping` records
3. Verify ESPN API is accessible
4. Check worker logs for settlement activity
