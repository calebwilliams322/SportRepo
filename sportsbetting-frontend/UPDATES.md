# Frontend Updates - Login Fix & Deposit Feature

## 🔧 Issues Fixed

### 1. "Failed to fetch" Login Error ✅
**Problem:** Opening `index.html` directly causes CORS errors

**Solution:** Serve via HTTP server instead

```bash
cd /Users/calebwilliams/SportRepo/sportsbetting-frontend
python3 -m http.server 8000
```

Then open: **http://localhost:8000**

**Why:** Browsers block `file://` from accessing `http://localhost:5192` (CORS)

---

### 2. Login Button Missing ✅
**Problem:** After navigating away from login page, no way to get back

**Solution:** Added green "Login / Register" button in top right
- Shows when you're NOT logged in
- Hides when you ARE logged in
- Appears again when you logout

---

## 🆕 New Features Added

### 1. Deposit Funds 💵

**Location:** Wallet page → "Deposit Funds" button

**How it works:**
1. Go to "Wallet" page
2. Click "💵 Deposit Funds"
3. Enter amount (e.g., $100)
4. Click "Confirm Deposit"

**Current limitation:** Backend doesn't have deposit endpoint yet, so it shows SQL command to run manually:

```sql
UPDATE "Wallets"
SET
  "Balance" = <new_balance>,
  "TotalDeposited" = "TotalDeposited" + <amount>,
  "LastUpdatedAt" = NOW()
WHERE "Id" = '<wallet_id>';
```

---

### 2. Initial Balance Selection (Registration)

**How it works:**
1. Register a new account
2. Modal pops up: "Set Your Starting Balance"
3. Enter desired amount (default: $1000)
4. Click "Create Wallet & Continue"

**Current limitation:** Still requires manual wallet creation via SQL (instructions shown in console)

---

## 📋 How To Use Now

### Option A: Test Account (Easiest)

**Just login with:**
- Email: `testbettor2@example.com`
- Password: `TestPassword123`

Already has wallet with $850 balance ✅

---

### Option B: Create New Account

1. **Start HTTP server:**
   ```bash
   cd /Users/calebwilliams/SportRepo/sportsbetting-frontend
   python3 -m http.server 8000
   ```

2. **Open browser:** http://localhost:8000

3. **Click "Register" tab**

4. **Fill in registration form**

5. **Modal appears:** "Set Your Starting Balance"
   - Enter amount (e.g., $1000)
   - Click "Create Wallet & Continue"

6. **Error appears with SQL command** - Copy it

7. **Run SQL command in terminal:**
   ```bash
   # The command will be in format:
   psql -U calebwilliams -d sportsbetting -c "INSERT INTO \"Wallets\" ..."
   ```

8. **Refresh page and login**

9. **Start betting!**

---

## 🎯 Complete Working Flow

### 1. Start Everything

**Terminal 1 - API:**
```bash
cd /Users/calebwilliams/SportRepo/SportsBetting/SportsBetting.API
dotnet run
```

**Terminal 2 - Frontend:**
```bash
cd /Users/calebwilliams/SportRepo/sportsbetting-frontend
python3 -m http.server 8000
```

**Optional - Terminal 3 - Worker:**
```bash
cd /Users/calebwilliams/SportRepo/SportsBettingListener/SportsBettingListener.Worker
dotnet run
```

### 2. Open Browser

http://localhost:8000

### 3. Login

Use test account or create new one

### 4. Browse & Bet

- Click "Events & Betting"
- Choose NFL or NBA
- Click event card
- Place bet

### 5. Manage Wallet

- Click "Wallet"
- See balance and stats
- Click "Deposit Funds" to add money

---

## 🐛 Known Limitations

### Must Run via HTTP Server
- ❌ Opening `index.html` directly → CORS error
- ✅ Opening `http://localhost:8000` → Works!

### Wallet Not Auto-Created
- Backend registration doesn't create wallet
- Must run SQL command manually
- Future: Add POST /api/wallets endpoint to backend

### Deposit Requires Manual SQL
- Deposit button shows SQL command
- Must run command in terminal
- Future: Add POST /api/wallets/{id}/deposit endpoint to backend

---

## 🔮 What's Next?

### Backend Changes Needed (Not in Frontend)

1. **Auto-create wallet on registration**
   - Modify `AuthController.cs` POST /auth/register
   - Create wallet automatically with default balance

2. **Add deposit endpoint**
   - Add POST /api/wallets/{id}/deposit
   - Add POST /api/wallets/{id}/withdraw

3. **Add create wallet endpoint**
   - Add POST /api/wallets
   - Allow creating wallets separately

### Frontend Already Ready For:
- ✅ Deposit UI built
- ✅ Withdrawal UI (similar to deposit)
- ✅ Initial balance selection
- ✅ All API calls structured

Just need backend endpoints!

---

## 📊 Summary

| Feature | Status | Notes |
|---------|--------|-------|
| **Login/Register** | ✅ Working | Use HTTP server |
| **Login Button** | ✅ Added | Top right corner |
| **Deposit UI** | ✅ Built | Shows SQL for now |
| **Initial Balance** | ✅ Built | Shows SQL for now |
| **Test Account** | ✅ Ready | Use testbettor2@example.com |
| **Events Browsing** | ✅ Working | NFL/NBA tabs |
| **Bet Placement** | ✅ Working | Single bets functional |
| **Bet Tracking** | ✅ Working | All status filters |

---

## 🎉 You Can Now:

1. ✅ Login without CORS errors (via HTTP server)
2. ✅ Navigate back to login anytime (top right button)
3. ✅ See deposit UI on wallet page
4. ✅ Get SQL commands for wallet operations
5. ✅ Set initial balance during registration
6. ✅ Use test account for instant access

**Everything works - just need backend endpoints for full automation!**
