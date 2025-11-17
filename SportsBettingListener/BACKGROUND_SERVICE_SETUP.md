# Background Service Setup Guide

## What is a Background Service?

A background service (daemon) runs continuously without user interaction. It's like having an automated assistant that works 24/7.

**For SportsBetting Worker, this means:**
- ✓ Runs automatically when your computer starts
- ✓ Updates odds every 5 minutes from The Odds API
- ✓ Monitors ESPN for game completions
- ✓ Auto-settles bets when games finish
- ✓ Keeps running even if you close your terminal
- ✓ Auto-restarts if it crashes

## Setup Instructions (macOS using launchd)

### 1. Copy the Service Configuration
```bash
sudo cp /tmp/com.sportsbetting.worker.plist ~/Library/LaunchAgents/
```

### 2. Set Correct Permissions
```bash
chmod 644 ~/Library/LaunchAgents/com.sportsbetting.worker.plist
```

### 3. Load and Start the Service
```bash
launchctl load ~/Library/LaunchAgents/com.sportsbetting.worker.plist
launchctl start com.sportsbetting.worker
```

### 4. Verify It's Running
```bash
launchctl list | grep sportsbetting
```

Expected output: Shows the service with a PID (process ID)

### 5. Check Logs
```bash
# View real-time logs
tail -f /Users/calebwilliams/SportRepo/SportsBettingListener/logs/worker-stdout.log

# View errors
tail -f /Users/calebwilliams/SportRepo/SportsBettingListener/logs/worker-stderr.log
```

## Managing the Service

### Stop the Service
```bash
launchctl stop com.sportsbetting.worker
```

### Restart the Service
```bash
launchctl stop com.sportsbetting.worker
launchctl start com.sportsbetting.worker
```

### Unload (Disable Auto-Start)
```bash
launchctl unload ~/Library/LaunchAgents/com.sportsbetting.worker.plist
```

### Reload After Configuration Changes
```bash
launchctl unload ~/Library/LaunchAgents/com.sportsbetting.worker.plist
launchctl load ~/Library/LaunchAgents/com.sportsbetting.worker.plist
```

## What the Service Does

**Every 5 Minutes:**
1. Fetches latest odds from The Odds API (DraftKings)
2. Updates odds for all scheduled events in database
3. Saves odds history for tracking line movements

**Continuously:**
1. Checks ESPN API for completed games
2. Matches completed games to events using ExternalEventMapping
3. Auto-settles all markets for completed events
4. Auto-settles all bets (winners get paid, losers recorded)
5. Processes payouts to user wallets

## Configuration Location

Service file: `~/Library/LaunchAgents/com.sportsbetting.worker.plist`
Worker config: `/Users/calebwilliams/SportRepo/SportsBettingListener/SportsBettingListener.Worker/appsettings.json`

## Current Settings

```json
{
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

## Monitoring

### Check if Service is Running
```bash
ps aux | grep SportsBettingListener.Worker
```

### View Recent Log Entries
```bash
tail -n 50 /Users/calebwilliams/SportRepo/SportsBettingListener/logs/worker-stdout.log
```

### Watch Logs in Real-Time
```bash
tail -f /Users/calebwilliams/SportRepo/SportsBettingListener/logs/worker-stdout.log
```

## Troubleshooting

### Service Won't Start
1. Check dotnet is at correct path: `/opt/homebrew/bin/dotnet`
2. Verify project builds: `cd SportsBettingListener.Worker && dotnet build`
3. Check service logs for errors

### Service Keeps Crashing
1. Check stderr log: `cat logs/worker-stderr.log`
2. Verify database connection string is correct
3. Ensure API key is valid (check remaining requests)

### Service Not Auto-Settling Bets
1. Verify `EnableAutoSettlement: true` in appsettings.json
2. Check that events have `ExternalEventMapping` records
3. Look for settlement logs in stdout

## Running Worker Only When API is Running

You mentioned wanting the worker to run only when the betting API is running. Here are your options:

### Option 1: Manual Control (Recommended for Development)
**What:** Start and stop both manually when you need them

```bash
# Terminal 1 - API
cd SportsBetting.API
dotnet run

# Terminal 2 - Worker
cd SportsBettingListener.Worker
dotnet run
```

**Pros:** Full control, see live logs, easy to debug
**Cons:** Must start both manually, stops when terminal closes

### Option 2: Screen Sessions (Middle Ground)
**What:** Run both in screen sessions you can detach from

```bash
# Start API
screen -S api
cd SportsBetting.API
dotnet run
# Ctrl+A, D (detach)

# Start Worker
screen -S worker
cd SportsBettingListener.Worker
dotnet run
# Ctrl+A, D (detach)

# Stop both when done
screen -X -S api quit
screen -X -S worker quit
```

**Pros:** Runs in background, survives terminal close, easy to check on
**Cons:** Doesn't survive reboot, manual start

### Option 3: Start/Stop Script (Automated)
**What:** Simple script to start/stop both together

Create `~/start-betting.sh`:
```bash
#!/bin/bash
echo "Starting SportsBetting API..."
screen -dmS api bash -c "cd ~/SportRepo/SportsBetting/SportsBetting.API && dotnet run"

echo "Starting Odds Worker..."
screen -dmS worker bash -c "cd ~/SportRepo/SportsBettingListener/SportsBettingListener.Worker && dotnet run"

echo "✓ Both services started in screen sessions"
echo "  View API: screen -r api"
echo "  View Worker: screen -r worker"
```

Create `~/stop-betting.sh`:
```bash
#!/bin/bash
echo "Stopping SportsBetting services..."
screen -X -S api quit
screen -X -S worker quit
echo "✓ Both services stopped"
```

**Usage:**
```bash
chmod +x ~/start-betting.sh ~/stop-betting.sh
~/start-betting.sh    # Start both
~/stop-betting.sh     # Stop both
```

**Pros:** One command starts both, one command stops both
**Cons:** Doesn't survive reboot

### Option 4: Background Service with Dependency (Advanced)
**What:** Worker background service that only runs if API is running

Modify `com.sportsbetting.worker.plist` to add:
```xml
<key>KeepAlive</key>
<dict>
    <key>OtherJobEnabled</key>
    <string>com.sportsbetting.api</string>
</dict>
```

This requires setting up API as a background service too.

**Pros:** Fully automated, survives reboot
**Cons:** Complex setup, both must be services

### Option 5: Embed Worker in API (Simplest for "Only Run Together")
**What:** Run worker as a hosted service inside the API process

Add to `SportsBetting.API/Program.cs`:
```csharp
// Add the worker as a hosted service in the API
builder.Services.AddHostedService<Worker>();
```

**Pros:** Single process, guaranteed to run together
**Cons:** API restart = worker restart (loses in-progress work)

## Recommendations by Use Case

| Use Case | Recommended Approach |
|----------|---------------------|
| **Development (daily coding)** | Option 1: Manual Control |
| **Development (testing over hours)** | Option 2: Screen Sessions |
| **Local testing with friends** | Option 3: Start/Stop Script |
| **Production deployment** | Option 4: Background Services |
| **Simplicity over separation** | Option 5: Embed in API |

## Production Recommendations

**For development (current setup):**
- ✓ Option 2 or 3: Screen sessions or start/stop script
- ✓ Logs visible in terminal or screen sessions
- ✓ Easy to start/stop both together

**For production deployment:**
- Use systemd on Linux servers (both API and Worker as services)
- Add log rotation (logrotate)
- Set up monitoring alerts (email on crash)
- Use paid API key for unlimited requests
- Add Redis for caching frequently accessed events
- Configure environment-specific appsettings
- Use Option 4: Background services with proper dependencies
