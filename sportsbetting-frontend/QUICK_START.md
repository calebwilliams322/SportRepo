# Quick Start Guide - 5 Minutes to Betting!

## Step 1: Start the API (30 seconds)

```bash
cd /Users/calebwilliams/SportRepo/SportsBetting/SportsBetting.API
dotnet run
```

**Wait for:** `Now listening on: http://localhost:5192`

---

## Step 2: Open the Frontend (10 seconds)

### Option A: Direct Open
```bash
open /Users/calebwilliams/SportRepo/sportsbetting-frontend/index.html
```

### Option B: Via HTTP Server (if Option A has CORS issues)
```bash
cd /Users/calebwilliams/SportRepo/sportsbetting-frontend
python3 -m http.server 8000
```
Then open: http://localhost:8000

---

## Step 3: Register an Account (1 minute)

1. Click **"Register"** tab
2. Fill in:
   - Username: `yourname`
   - Email: `you@example.com`
   - First Name: `Your`
   - Last Name: `Name`
   - Password: `password123`
3. Click **"Register"**

**Note:** You'll need to manually create a wallet (see below)

---

## Step 4: Create Your Wallet (30 seconds)

Open a new terminal and run:

```bash
# Replace USER_ID with your user ID from the console log
psql -U calebwilliams -d sportsbetting -c "
INSERT INTO \"Wallets\" (
  \"Id\", \"UserId\", \"CreatedAt\", \"LastUpdatedAt\",
  \"Balance\", \"Currency\",
  \"TotalBet\", \"TotalBetCurrency\",
  \"TotalDeposited\", \"TotalDepositedCurrency\",
  \"TotalWithdrawn\", \"TotalWithdrawnCurrency\",
  \"TotalWon\", \"TotalWonCurrency\"
) VALUES (
  'USER_ID_HERE',  -- Replace with your user ID
  'USER_ID_HERE',  -- Same user ID
  NOW(), NOW(),
  1000.00, 'USD',
  0.00, 'USD',
  1000.00, 'USD',
  0.00, 'USD',
  0.00, 'USD'
);
"
```

**Or use the test account:**
- Email: `testbettor2@example.com`
- Password: `TestPassword123`
- Balance: $850

---

## Step 5: Start Betting! (3 minutes)

1. **Click "Events & Betting"** in sidebar
2. **Choose a sport** (🏈 NFL or 🏀 NBA tabs)
3. **Click an event card** to see betting options
4. **Select an outcome** (team to win)
5. **Enter your stake** (e.g., $50)
6. **Click "Place Bet"**
7. **Get confirmation** with ticket number!

---

## 🎉 That's It!

You now have:
- ✅ Account created
- ✅ Wallet funded ($1000)
- ✅ Events loaded with live odds
- ✅ Able to place bets
- ✅ Track bets in "My Bets"
- ✅ View wallet in "Wallet" page

---

## Optional: Run the Worker (for auto-updates)

In a **third terminal:**

```bash
cd /Users/calebwilliams/SportRepo/SportsBettingListener/SportsBettingListener.Worker
dotnet run
```

**What it does:**
- Updates odds every 5 minutes
- Auto-settles bets when games complete
- Keeps events fresh

---

## Troubleshooting

### "No events showing"
Run the worker first to sync events from The Odds API:
```bash
cd SportsBettingListener.Worker
dotnet run
```
Wait 1-2 minutes, then refresh the frontend.

### "Wallet not found"
Create the wallet using the SQL command in Step 4.

### "CORS error"
Use Option B (HTTP server) instead of opening the file directly.

### "API not responding"
Make sure the API is running on port 5192:
```bash
curl http://localhost:5192/api/events
```

---

## Pro Tips

### View API Calls in Console
Open browser DevTools (F12) → Console → See all API requests and responses

### Place Bets via Console
```javascript
// After logging in
const events = await API.Events.getEventsBySport('nfl', 'Scheduled');
const market = await API.Events.getMarket(events[0].markets[0].id);
const bet = await API.Bets.placeSingleBet({
    eventId: events[0].id,
    marketId: market.id,
    outcomeId: market.outcomes[0].id,
    stake: 50.00
});
console.log('Bet placed:', bet);
```

### Check Your Balance Anytime
```javascript
const user = API.Auth.getCurrentUser();
const wallet = await API.Wallets.getUserWallet(user.id);
console.log('Balance:', wallet.balance);
```

---

## What's Next?

### Explore Features
- Browse different sports (NFL/NBA)
- Filter by game status
- View bet history
- Check wallet stats

### Place Different Bet Types
- Try different outcomes (spreads, totals)
- Place multiple bets
- Track your performance

### Monitor Your Bets
- Go to "My Bets"
- Filter by status (Pending/Won/Lost)
- See settlement results

---

## Need Help?

- **Frontend Guide:** `README.md` in this folder
- **API Guide:** `../SportsBetting/API_USAGE_GUIDE.md`
- **System Architecture:** `../SportsBettingListener/ARCHITECTURE.md`
- **Complete Summary:** `../COMPLETE_SYSTEM_SUMMARY.md`

---

**Happy Betting! 🎲**
