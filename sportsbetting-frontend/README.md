# SportsBetting Frontend

Professional sports betting interface with live odds, real-time betting, and comprehensive bet tracking.

## ✨ Features

### 🎯 Core Functionality
- **User Authentication** - Register, login, session management
- **Live Events** - Browse NFL and NBA games with live odds
- **Betting** - Place single and parlay bets
- **Bet Tracking** - View all bets (pending, won, lost)
- **Wallet Management** - Track balance, deposits, winnings

### 🎨 Design
- **Playing Card Style** - Events displayed as premium betting cards
- **Dark Mode** - Professional dark theme with gradient accents
- **Responsive** - Works on desktop, tablet, and mobile
- **Sidebar Navigation** - Clean left-side menu
- **Sport Tabs** - Easy switching between NFL and NBA

### 🔌 API Integration
- **All Endpoints Exposed** - Complete API client in `js/api.js`
- **Auth Endpoints** - Login, register, refresh token
- **Events Endpoints** - Browse events, get odds, filter by sport/status
- **Bets Endpoints** - Place single/parlay bets, view bet history
- **Wallet Endpoints** - Get balance, deposit, withdraw
- **Admin Endpoints** - User management, bet settlement (if admin)
- **Exchange Endpoints** - P2P betting (if enabled)

## 🚀 Quick Start

### Prerequisites
- SportsBetting API running on `http://localhost:5192`
- Modern web browser (Chrome, Firefox, Safari, Edge)

### Installation

1. **Open in Browser**
   ```bash
   # Navigate to frontend directory
   cd /Users/calebwilliams/SportRepo/sportsbetting-frontend

   # Option 1: Open directly in browser
   open index.html

   # Option 2: Serve with Python (recommended for avoiding CORS)
   python3 -m http.server 8000
   # Then open http://localhost:8000
   ```

2. **That's it!** No build tools required - pure HTML/CSS/JavaScript

### First Time Setup

1. **Start the API**
   ```bash
   cd /Users/calebwilliams/SportRepo/SportsBetting/SportsBetting.API
   dotnet run
   ```

2. **Open Frontend**
   - Open `index.html` in your browser
   - Or serve via HTTP server: `python3 -m http.server 8000`

3. **Register an Account**
   - Click "Register" tab
   - Fill in your details
   - Submit to create account

4. **Start Betting!**
   - Browse events (NFL/NBA tabs)
   - Click event cards to see odds
   - Place bets and track them

## 📁 Project Structure

```
sportsbetting-frontend/
├── index.html              # Main HTML file
├── css/
│   └── styles.css          # All styles (playing card design)
├── js/
│   ├── api.js              # Complete API client (ALL endpoints)
│   ├── auth.js             # Authentication logic
│   ├── events.js           # Events browsing & betting
│   ├── bets.js             # Bet history & tracking
│   ├── wallet.js           # Wallet management
│   └── app.js              # Main app navigation
└── README.md               # This file
```

## 🔌 API Endpoints Exposed

All endpoints are fully documented in `js/api.js`. Here's what's available:

### Authentication (`API.Auth`)
```javascript
// Register new user
await API.Auth.register({
    username: 'johndoe',
    email: 'john@example.com',
    firstName: 'John',
    lastName: 'Doe',
    password: 'password123'
});

// Login
await API.Auth.login({
    email: 'john@example.com',
    password: 'password123'
});

// Refresh token
await API.Auth.refreshToken(refreshToken);

// Logout
API.Auth.logout();

// Check auth status
API.Auth.isAuthenticated();
```

### Events (`API.Events`)
```javascript
// Get all events
await API.Events.getEvents({
    status: 'Scheduled',  // 'Scheduled' | 'InProgress' | 'Completed'
    page: 1,
    pageSize: 20
});

// Get event by ID
await API.Events.getEventById(eventId);

// Get market with odds
await API.Events.getMarket(marketId);

// Get events by sport (helper)
await API.Events.getEventsBySport('nfl', 'Scheduled');
```

### Bets (`API.Bets`)
```javascript
// Place single bet
await API.Bets.placeSingleBet({
    eventId: 'event-guid',
    marketId: 'market-guid',
    outcomeId: 'outcome-guid',
    stake: 100.00
});

// Place parlay bet
await API.Bets.placeParlayBet({
    stake: 50.00,
    legs: [
        { eventId: '...', marketId: '...', outcomeId: '...' },
        { eventId: '...', marketId: '...', outcomeId: '...' }
    ]
});

// Get user's bets
await API.Bets.getUserBets(userId, {
    status: 'Pending',  // Optional filter
    page: 1,
    pageSize: 20
});

// Get bet by ID
await API.Bets.getBetById(betId);
```

### Wallets (`API.Wallets`)
```javascript
// Get user's wallet
await API.Wallets.getUserWallet(userId);

// Get wallet by ID
await API.Wallets.getWalletById(walletId);

// Deposit funds (if endpoint exists)
await API.Wallets.deposit(walletId, 100.00);

// Withdraw funds (if endpoint exists)
await API.Wallets.withdraw(walletId, 50.00);
```

### Exchange (`API.Exchange`)
```javascript
// Place exchange bet (back/lay)
await API.Exchange.placeExchangeBet({
    marketId: 'market-guid',
    outcomeId: 'outcome-guid',
    side: 'Back',  // 'Back' | 'Lay'
    odds: 2.5,
    stake: 100.00
});

// Get order book
await API.Exchange.getOrderBook(marketId);

// Cancel bet
await API.Exchange.cancelBet(betId);
```

### Admin (`API.Admin`)
```javascript
// Get all users (admin only)
await API.Admin.getAllUsers();

// Get all bets (admin only)
await API.Admin.getAllBets({ status: 'Pending' });

// Manually settle bet (admin only)
await API.Admin.settleBet(betId, 'Won');
```

### Settlement (`API.Settlement`)
```javascript
// Manually trigger settlement
await API.Settlement.settleEvent(eventId, {
    homeScore: 28,
    awayScore: 24
});

// Get settlement history
await API.Settlement.getSettlementHistory();
```

### Revenue (`API.Revenue`)
```javascript
// Get house revenue stats
await API.Revenue.getRevenueStats({
    startDate: '2025-01-01',
    endDate: '2025-01-31'
});
```

## 🎮 Usage Examples

### Example 1: Place a Bet via Console

Open browser console and try:

```javascript
// Login first
await API.Auth.login({
    email: 'testbettor2@example.com',
    password: 'TestPassword123'
});

// Get NFL events
const events = await API.Events.getEventsBySport('nfl', 'Scheduled');
console.log('NFL Events:', events);

// Get first event's market
const market = await API.Events.getMarket(events[0].markets[0].id);
console.log('Market odds:', market);

// Place bet on first outcome
const bet = await API.Bets.placeSingleBet({
    eventId: events[0].id,
    marketId: market.id,
    outcomeId: market.outcomes[0].id,
    stake: 50.00
});
console.log('Bet placed:', bet);
```

### Example 2: Check Wallet Balance

```javascript
const user = API.Auth.getCurrentUser();
const wallet = await API.Wallets.getUserWallet(user.id);
console.log('Balance:', wallet.balance);
console.log('Total Won:', wallet.totalWon);
console.log('Net P/L:', wallet.netProfitLoss);
```

### Example 3: View All Pending Bets

```javascript
const user = API.Auth.getCurrentUser();
const bets = await API.Bets.getUserBets(user.id, { status: 'Pending' });
console.log('Pending Bets:', bets);
```

## 🎨 Customization

### Change Colors

Edit `css/styles.css`:

```css
:root {
    --primary: #10b981;         /* Main green color */
    --secondary: #3b82f6;       /* Blue accent */
    --danger: #ef4444;          /* Red for losses */
    --dark: #1f2937;            /* Dark background */
}
```

### Change API Base URL

Edit `js/api.js`:

```javascript
const API_BASE_URL = 'http://your-api-url.com/api';
```

### Add New Pages

1. Add page div to `index.html`:
   ```html
   <div class="page" id="page-newpage">
       <!-- Your content -->
   </div>
   ```

2. Add nav item to sidebar:
   ```html
   <a href="#" class="nav-item" data-page="newpage">
       <span class="icon">🎯</span>
       <span>New Page</span>
   </a>
   ```

3. Add page logic to `js/app.js`:
   ```javascript
   case 'newpage':
       loadNewPageData();
       break;
   ```

## 🐛 Troubleshooting

### API Calls Failing (CORS Error)

**Problem:** Browser blocks requests due to CORS

**Solution:** Serve frontend via HTTP server instead of opening file directly:
```bash
python3 -m http.server 8000
# Open http://localhost:8000
```

Or enable CORS in the API (already done in `SportsBetting.API/Program.cs`)

### Wallet Not Found After Registration

**Problem:** Registration doesn't auto-create wallet

**Solution:** Manually create wallet in database:
```sql
INSERT INTO "Wallets" (
  "Id", "UserId", "CreatedAt", "LastUpdatedAt",
  "Balance", "Currency",
  "TotalBet", "TotalBetCurrency",
  "TotalDeposited", "TotalDepositedCurrency",
  "TotalWithdrawn", "TotalWithdrawnCurrency",
  "TotalWon", "TotalWonCurrency"
) VALUES (
  'user-id-here',
  'user-id-here',
  NOW(), NOW(),
  1000.00, 'USD',
  0.00, 'USD',
  1000.00, 'USD',
  0.00, 'USD',
  0.00, 'USD'
);
```

### No Events Showing

**Problem:** Worker hasn't synced events yet

**Solution:** Run the worker to sync events:
```bash
cd /Users/calebwilliams/SportRepo/SportsBettingListener/SportsBettingListener.Worker
dotnet run
```

Wait 1-2 minutes for initial sync, then refresh frontend.

### Odds Not Updating

**Problem:** Worker not running or API key exhausted

**Solution:**
1. Check worker is running
2. Check API key has remaining requests
3. Manually refresh events in frontend (click "Refresh Odds" button)

## 📊 Tech Stack

| Technology | Purpose |
|-----------|---------|
| **HTML5** | Structure |
| **CSS3** | Styling (gradients, animations) |
| **Vanilla JavaScript** | Logic (no frameworks) |
| **Fetch API** | HTTP requests |
| **LocalStorage** | Auth token persistence |
| **CSS Grid/Flexbox** | Layout |

## 🚀 Performance

- **No Build Required** - Instant loading
- **Minimal Dependencies** - Pure vanilla JS
- **Fast API Calls** - Direct fetch, no overhead
- **Local State** - Auth token cached
- **Lazy Loading** - Pages load content on demand

## 🔐 Security Notes

- **JWT Tokens** - Stored in localStorage (consider httpOnly cookies for production)
- **HTTPS Required** - For production deployment
- **Input Validation** - Always validate on backend (API handles this)
- **CORS Enabled** - API allows frontend origin

## 📈 Future Enhancements

### Short Term
- [x] User registration/login
- [x] Browse events (NFL/NBA)
- [x] Place bets
- [x] View bet history
- [x] Wallet management

### Next Steps
- [ ] Live odds updates (WebSocket)
- [ ] Bet slip for parlay building
- [ ] Deposit/withdraw UI (if endpoints added)
- [ ] Exchange betting UI (P2P matching)
- [ ] Live score updates
- [ ] Push notifications for bet results
- [ ] Charts for betting history
- [ ] Social features (leaderboard, sharing)

## 📝 License

This frontend is part of the SportsBetting system.

## 🤝 Support

For issues or questions:
1. Check browser console for errors
2. Verify API is running on port 5192
3. Check network tab in DevTools
4. Review API endpoint documentation in `js/api.js`

## 🎯 Quick Reference

### Test User (from API testing)
- **Email:** testbettor2@example.com
- **Password:** TestPassword123
- **Balance:** $850 (after test bets)

### API Running
```bash
cd /Users/calebwilliams/SportRepo/SportsBetting/SportsBetting.API
dotnet run
```

### Worker Running (for odds updates)
```bash
cd /Users/calebwilliams/SportRepo/SportsBettingListener/SportsBettingListener.Worker
dotnet run
```

### Frontend Running
```bash
cd /Users/calebwilliams/SportRepo/sportsbetting-frontend
python3 -m http.server 8000
# Open http://localhost:8000
```

---

**Built with ❤️ using vanilla JavaScript - No frameworks, just pure web technologies!**
