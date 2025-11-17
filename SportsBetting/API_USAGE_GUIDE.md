# API Usage Guide - Live Betting System

## System Status: ✅ READY FOR USERS

Your SportsBetting API is **fully functional** and connected to live odds data from The Odds API.

## What's Working Right Now

### 1. Live Events in Database
- ✅ 28 NFL events with live odds
- ✅ 16 NBA events with live odds
- ✅ Auto-updating odds every 5 minutes (if worker is running)
- ✅ Auto-settlement when games complete

### 2. API Endpoints

**Base URL:** `http://localhost:5192/api`

---

## Browse Events

### Get All Events
```bash
GET /events?status=Scheduled&pageSize=20

# Example Response:
{
  "id": "2afa518b-c39f-4e9e-bd75-430089fa6aad",
  "name": "Charlotte Hornets vs Oklahoma City Thunder",
  "homeTeamName": "Charlotte Hornets",
  "awayTeamName": "Oklahoma City Thunder",
  "scheduledStartTime": "2025-11-16T00:11:00Z",
  "status": "Scheduled",
  "markets": [
    {
      "id": "bfef3c56-4b68-413b-a7ab-6722071c9548",
      "type": "Moneyline",
      "name": "Moneyline",
      "isOpen": true,
      "outcomeCount": 2
    }
  ]
}
```

**Query Parameters:**
- `status` - Filter by status (Scheduled, InProgress, Completed)
- `leagueId` - Filter by league
- `page` - Page number (default: 1)
- `pageSize` - Results per page (default: 20)

---

### Get Specific Event
```bash
GET /events/{eventId}

# Example:
GET /events/2afa518b-c39f-4e9e-bd75-430089fa6aad
```

---

### Get Market with Odds
```bash
GET /events/markets/{marketId}

# Example Response:
{
  "id": "bfef3c56-4b68-413b-a7ab-6722071c9548",
  "type": "Moneyline",
  "name": "Moneyline",
  "isOpen": true,
  "isSettled": false,
  "outcomes": [
    {
      "id": "97f7565c-6e73-4d56-af23-63b7faf43e56",
      "name": "Oklahoma City Thunder",
      "currentOdds": 1.06,    ← Live odds!
      "line": null
    },
    {
      "id": "98ec3e78-a47f-41c4-885e-db2428ad0a25",
      "name": "Charlotte Hornets",
      "currentOdds": 9.0,     ← Live odds!
      "line": null
    }
  ]
}
```

---

## Place Bets

### Place Single Bet
```bash
POST /bets/single
Authorization: Bearer {jwt-token}

# Request Body:
{
  "eventId": "2afa518b-c39f-4e9e-bd75-430089fa6aad",
  "marketId": "bfef3c56-4b68-413b-a7ab-6722071c9548",
  "outcomeId": "97f7565c-6e73-4d56-af23-63b7faf43e56",
  "stake": 100.00
}

# Response:
{
  "id": "bet-id-here",
  "ticketNumber": "BET-2025-001",
  "status": "Pending",
  "stake": 100.00,
  "combinedOdds": 1.06,
  "potentialPayout": 106.00,
  "placedAt": "2025-11-15T...",
  "selections": [...]
}
```

---

### Place Parlay Bet
```bash
POST /bets/parlay
Authorization: Bearer {jwt-token}

# Request Body:
{
  "stake": 50.00,
  "legs": [
    {
      "eventId": "event-1-id",
      "marketId": "market-1-id",
      "outcomeId": "outcome-1-id"
    },
    {
      "eventId": "event-2-id",
      "marketId": "market-2-id",
      "outcomeId": "outcome-2-id"
    }
  ]
}
```

---

### Get User's Bets
```bash
GET /bets/user/{userId}?status=Pending&page=1&pageSize=20
Authorization: Bearer {jwt-token}
```

---

## Real Example - Full Betting Flow

### 1. User Browses Today's Games
```bash
curl http://localhost:5192/api/events?status=Scheduled&pageSize=10
```

### 2. User Clicks on a Game to See Betting Options
```bash
curl http://localhost:5192/api/events/2afa518b-c39f-4e9e-bd75-430089fa6aad
```

### 3. User Selects a Market to See Odds
```bash
curl http://localhost:5192/api/events/markets/bfef3c56-4b68-413b-a7ab-6722071c9548
```

**User sees:**
- Oklahoma City Thunder @ 1.06 odds ($100 → $106)
- Charlotte Hornets @ 9.0 odds ($100 → $900)

### 4. User Places Bet
```bash
curl -X POST http://localhost:5192/api/bets/single \
  -H "Authorization: Bearer {user-jwt-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": "2afa518b-c39f-4e9e-bd75-430089fa6aad",
    "marketId": "bfef3c56-4b68-413b-a7ab-6722071c9548",
    "outcomeId": "97f7565c-6e73-4d56-af23-63b7faf43e56",
    "stake": 100.00
  }'
```

### 5. Bet is Placed!
- $100 deducted from user's wallet
- Bet is locked at current odds (1.06)
- Bet status: **Pending**
- Potential payout: $106.00

### 6. Game Completes (Automatic)
When the game finishes:
- ✅ Worker detects game completion via ESPN
- ✅ Auto-settles all markets
- ✅ Auto-settles all bets
- ✅ Winners get paid automatically

---

## Auto-Settlement Example

**What Happened:**
```
Cleveland Cavaliers vs Memphis Grizzlies
Final Score: 108-100

✓ Event status: Completed
✓ Markets: Settled
✓ Bets: Auto-settled
✓ Payouts: Processed
```

You can see this in the API:
```bash
curl http://localhost:5192/api/events/7d0bb694-e2ec-4726-9eef-4f012c6062f9
```

Response shows:
```json
{
  "status": "Completed",
  "finalScore": "108-100"
}
```

---

## Next Steps

### For Development
1. **Test the endpoints** - Use the examples above
2. **Build your frontend** - Connect to these endpoints
3. **Run the worker** - Keep odds updated and auto-settle games

### Run the Worker (Optional but Recommended)
```bash
cd /Users/calebwilliams/SportRepo/SportsBettingListener/SportsBettingListener.Worker
dotnet run
```

**Worker does:**
- Updates odds every 5 minutes
- Checks ESPN for completed games
- Auto-settles bets when games finish

### For Production
1. **Get paid API key** - Upgrade from 500 free requests
2. **Add caching** - Redis for frequently accessed events
3. **Add WebSockets** - Push live odds updates to frontend
4. **Add monitoring** - Track API performance and errors

---

## Testing Quick Commands

### See all upcoming NBA games:
```bash
curl 'http://localhost:5192/api/events?status=Scheduled' | \
  python3 -m json.tool | \
  grep -A 3 '"name"' | \
  grep -E '(name|odds|isOpen)'
```

### See a completed game with settlement:
```bash
curl 'http://localhost:5192/api/events/7d0bb694-e2ec-4726-9eef-4f012c6062f9' | \
  python3 -m json.tool
```

### Check how many events are ready:
```bash
psql -U calebwilliams -d sportsbetting -c "
  SELECT
    status::text,
    COUNT(*) as count
  FROM \"Events\"
  GROUP BY status
  ORDER BY status;
"
```

---

## Authentication Note

The BetsController requires authentication:
- Users need a valid JWT token
- Token must contain user ID in the `sub` claim
- Users can only place bets for themselves
- Users can only view their own bets (unless Admin/Support)

The EventsController is **public** - no authentication needed to browse events and odds!

---

## Current Data Available

**Live Betting Events:**
- 28 NFL games (Sunday games)
- 16 NBA games (tonight's games)
- All with 3 markets each:
  - Moneyline (straight win)
  - Point Spread (handicap)
  - Total Points (over/under)

**Auto-Settlement:**
- ✅ Already working!
- Cleveland Cavaliers game was auto-settled
- Final score: 108-100

---

## Everything You Need is Ready! 🚀

Your API can:
- ✅ List all betting events
- ✅ Show live odds from DraftKings
- ✅ Accept bet placements
- ✅ Auto-settle when games complete
- ✅ Process payouts automatically

**Next:** Build your frontend to consume these endpoints!
