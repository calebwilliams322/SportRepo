# SportsBetting Platform - Complete Architecture Diagram

## System Architecture with The Odds API Integration

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                          EXTERNAL DATA SOURCE                               │
│                                                                             │
│                     ┌─────────────────────────────┐                        │
│                     │    The Odds API             │                        │
│                     │  (api.the-odds-api.com)    │                        │
│                     │                             │                        │
│                     │  - Live sporting events     │                        │
│                     │  - Real-time odds           │                        │
│                     │  - Multiple sports          │                        │
│                     │  - Multiple bookmakers      │                        │
│                     └─────────────────────────────┘                        │
│                                  │                                          │
│                                  │ HTTPS REST API                           │
│                                  │ (every 5 minutes)                        │
│                                  ▼                                          │
└─────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                     SPORTSBETTINGLISTENER SERVICE                           │
│                     (New Worker Service Project)                            │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Worker.cs (Background Service)                                     │   │
│  │  - Runs continuously                                                │   │
│  │  - Syncs every 5 minutes                                            │   │
│  │  - Handles multiple sports                                          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                  │                                          │
│                                  ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  OddsApiClient.cs                                                   │   │
│  │  - Fetch events from API                                            │   │
│  │  - Parse JSON responses                                             │   │
│  │  - Handle rate limits                                               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                  │                                          │
│                                  ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Mappers (EventMapper, MarketMapper)                                │   │
│  │  - Map OddsApiEvent → Event                                         │   │
│  │  - Map markets (h2h, spreads, totals)                               │   │
│  │  - Map odds to CurrentOdds field                                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                  │                                          │
│                                  ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  EventSyncService.cs                                                │   │
│  │  - Check if event exists (by ExternalId)                            │   │
│  │  - Create new events + markets + outcomes                           │   │
│  │  - Update existing event odds                                       │   │
│  │  - SaveChangesAsync()                                               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                  │                                          │
└──────────────────────────────────┼──────────────────────────────────────────┘
                                   │
                                   │ Direct Database Access
                                   │ (via shared DbContext)
                                   ▼


┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                            POSTGRESQL DATABASE                              │
│                                                                             │
│  ┌─────────────┬─────────────┬─────────────┬─────────────┬──────────────┐  │
│  │   Events    │   Markets   │  Outcomes   │    Teams    │   Sports     │  │
│  ├─────────────┼─────────────┼─────────────┼─────────────┼──────────────┤  │
│  │ Id          │ Id          │ Id          │ Id          │ Id           │  │
│  │ ExternalId  │ ExternalId  │ ExternalId  │ Name        │ Name         │  │
│  │ HomeTeamId  │ EventId     │ MarketId    │ ExternalName│ ExternalKey  │  │
│  │ AwayTeamId  │ Name        │ Description │             │              │  │
│  │ Status      │ MarketType  │ CurrentOdds │             │              │  │
│  │ StartTime   │ Status      │ LastUpdate  │             │              │  │
│  │ LastSynced  │ LastSynced  │             │             │              │  │
│  └─────────────┴─────────────┴─────────────┴─────────────┴──────────────┘  │
│                                                                             │
│  ┌─────────────┬─────────────┬─────────────┬─────────────┬──────────────┐  │
│  │    Bets     │   Users     │  Wallets    │ BetMatches  │ HouseRevenue │  │
│  ├─────────────┼─────────────┼─────────────┼─────────────┼──────────────┤  │
│  │ Id          │ Id          │ Id          │ Id          │ Id           │  │
│  │ UserId      │ Username    │ UserId      │ BackBetId   │ PeriodStart  │  │
│  │ BetMode     │ Email       │ Available   │ LayBetId    │ BookRevenue  │  │
│  │ Stake       │ Role        │ Locked      │ WinnerId    │ ExchRevenue  │  │
│  │ Status      │ Statistics  │             │ Commission  │ TotalRevenue │  │
│  └─────────────┴─────────────┴─────────────┴─────────────┴──────────────┘  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                   ▲
                                   │
                                   │ Read/Write
                                   │
┌──────────────────────────────────┼──────────────────────────────────────────┐
│                                  │                                          │
│                      SPORTSBETTING API SERVICE                              │
│                      (Existing ASP.NET Core API)                            │
│                                                                             │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Controllers                                                        │   │
│  │  ┌──────────────┬──────────────┬──────────────┬───────────────┐   │   │
│  │  │ MarketCtrl   │  BetCtrl     │ WalletCtrl   │ SettlementCtrl│   │   │
│  │  ├──────────────┼──────────────┼──────────────┼───────────────┤   │   │
│  │  │ GET /markets │ POST /bets   │ POST /deposit│ POST /settle  │   │   │
│  │  │ - Lists all  │ - Sportsbook │ GET /wallet  │ - Admin only  │   │   │
│  │  │   events     │ - Exchange   │              │ - Auto revenue│   │   │
│  │  │ - With API   │   (Back/Lay) │              │               │   │   │
│  │  │   odds       │              │              │               │   │   │
│  │  └──────────────┴──────────────┴──────────────┴───────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                  │                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  Services                                                           │   │
│  │  ┌──────────────┬──────────────┬──────────────┬───────────────┐   │   │
│  │  │ WalletSvc    │ SettleSvc    │ BetMatchSvc  │ RevenueSvc    │   │   │
│  │  │ MatchingSvc  │ CommissionSvc│              │               │   │   │
│  │  └──────────────┴──────────────┴──────────────┴───────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                  │                                          │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  SignalR Hub (Real-time)                                            │   │
│  │  - OrderBookHub: /hubs/orderbook                                    │   │
│  │  - Push updates when odds change                                    │   │
│  │  - Notify users of bet matches                                      │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                  │                                          │
└──────────────────────────────────┼──────────────────────────────────────────┘
                                   │
                                   │ HTTP/WebSocket
                                   ▼


┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                            CLIENT APPLICATIONS                              │
│                                                                             │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐               │
│  │  Web App       │  │  Mobile App    │  │  Admin Panel   │               │
│  │  (React)       │  │  (iOS/Android) │  │  (React)       │               │
│  ├────────────────┤  ├────────────────┤  ├────────────────┤               │
│  │ - View markets │  │ - Place bets   │  │ - Settle events│               │
│  │ - Place bets   │  │ - Live odds    │  │ - View revenue │               │
│  │ - Live odds    │  │ - Wallet mgmt  │  │ - Manage users │               │
│  │ - Order book   │  │                │  │                │               │
│  └────────────────┘  └────────────────┘  └────────────────┘               │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow: From The Odds API to User Bet

### Step-by-Step Flow Diagram

```
1. THE ODDS API
   ┌─────────────────────────────────────────┐
   │ NFL Game: Chiefs vs Raiders             │
   │ Start: 2025-11-20 18:00 UTC            │
   │                                         │
   │ Moneyline:                              │
   │   Chiefs: 1.45 (DraftKings)            │
   │   Raiders: 2.90 (DraftKings)           │
   │                                         │
   │ Spread:                                 │
   │   Chiefs -7.5: 1.91                    │
   │   Raiders +7.5: 1.91                   │
   └─────────────────────────────────────────┘
                    │
                    │ HTTP GET every 5 minutes
                    ▼
2. LISTENER: OddsApiClient.FetchEventsAsync()
   ┌─────────────────────────────────────────┐
   │ Parse JSON → OddsApiEvent object        │
   │ {                                       │
   │   id: "abc123",                         │
   │   home_team: "Chiefs",                  │
   │   away_team: "Raiders",                 │
   │   commence_time: "2025-11-20T18:00Z",  │
   │   bookmakers: [...]                     │
   │ }                                       │
   └─────────────────────────────────────────┘
                    │
                    ▼
3. LISTENER: EventMapper.MapToEventAsync()
   ┌─────────────────────────────────────────┐
   │ Create/lookup Sport ("NFL")             │
   │ Create/lookup Teams (Chiefs, Raiders)   │
   │ Create Event:                           │
   │   ExternalId = "abc123"                 │
   │   Status = Scheduled                    │
   │   StartTime = 2025-11-20 18:00         │
   └─────────────────────────────────────────┘
                    │
                    ▼
4. LISTENER: MarketMapper.MapMarketsForEvent()
   ┌─────────────────────────────────────────┐
   │ Create Market: "Moneyline"              │
   │   Outcome 1: "Chiefs" - Odds: 1.45     │
   │   Outcome 2: "Raiders" - Odds: 2.90    │
   │                                         │
   │ Create Market: "Point Spread"           │
   │   Outcome 1: "Chiefs -7.5" - Odds: 1.91│
   │   Outcome 2: "Raiders +7.5" - Odds: 1.91│
   └─────────────────────────────────────────┘
                    │
                    ▼
5. LISTENER: EventSyncService.SyncEventAsync()
   ┌─────────────────────────────────────────┐
   │ Check if exists: SELECT ... WHERE       │
   │   ExternalId = "abc123"                 │
   │                                         │
   │ IF NOT EXISTS:                          │
   │   INSERT INTO Events (...)              │
   │   INSERT INTO Markets (...)             │
   │   INSERT INTO Outcomes (...)            │
   │                                         │
   │ ELSE:                                   │
   │   UPDATE Outcomes SET CurrentOdds = ... │
   └─────────────────────────────────────────┘
                    │
                    │ SaveChangesAsync()
                    ▼
6. DATABASE
   ┌─────────────────────────────────────────┐
   │ Events Table:                           │
   │ ┌─────────────────────────────────────┐ │
   │ │ Id: guid-1                          │ │
   │ │ ExternalId: "abc123"                │ │
   │ │ HomeTeam: Chiefs                    │ │
   │ │ AwayTeam: Raiders                   │ │
   │ │ Status: Scheduled                   │ │
   │ │ LastSyncedAt: 2025-11-15 12:30     │ │
   │ └─────────────────────────────────────┘ │
   │                                         │
   │ Outcomes Table:                         │
   │ ┌─────────────────────────────────────┐ │
   │ │ Description: "Chiefs"               │ │
   │ │ CurrentOdds: 1.45                   │ │
   │ │ LastOddsUpdate: 2025-11-15 12:30   │ │
   │ └─────────────────────────────────────┘ │
   └─────────────────────────────────────────┘
                    │
                    │ Query
                    ▼
7. API: MarketController.GetMarkets()
   ┌─────────────────────────────────────────┐
   │ GET /api/markets                        │
   │                                         │
   │ SELECT * FROM Events                    │
   │ INCLUDE Markets, Outcomes               │
   │ WHERE Status = Scheduled                │
   │                                         │
   │ Return JSON:                            │
   │ [                                       │
   │   {                                     │
   │     eventId: "guid-1",                  │
   │     homeTeam: "Chiefs",                 │
   │     markets: [                          │
   │       {                                 │
   │         name: "Moneyline",              │
   │         outcomes: [                     │
   │           {name: "Chiefs", odds: 1.45}, │
   │           {name: "Raiders", odds: 2.90} │
   │         ]                               │
   │       }                                 │
   │     ]                                   │
   │   }                                     │
   │ ]                                       │
   └─────────────────────────────────────────┘
                    │
                    │ HTTP Response
                    ▼
8. USER: Web/Mobile App
   ┌─────────────────────────────────────────┐
   │ Display:                                │
   │ ┌─────────────────────────────────────┐ │
   │ │ Chiefs vs Raiders                   │ │
   │ │ Nov 20, 6:00 PM                     │ │
   │ │                                     │ │
   │ │ Moneyline:                          │ │
   │ │ [ Chiefs 1.45 ] [ Raiders 2.90 ]   │ │
   │ │                                     │ │
   │ │ Your Bet: $100 on Chiefs            │ │
   │ │ Potential Win: $145                 │ │
   │ │                                     │ │
   │ │      [Place Bet (Sportsbook)]       │ │
   │ └─────────────────────────────────────┘ │
   └─────────────────────────────────────────┘
                    │
                    │ POST /api/bets/sportsbook
                    ▼
9. API: BetController.PlaceSportsbookBet()
   ┌─────────────────────────────────────────┐
   │ Validate odds still valid               │
   │ Lock wallet ($100)                      │
   │ Create Bet:                             │
   │   UserId: user-123                      │
   │   OutcomeId: outcome-1 (Chiefs)         │
   │   Stake: $100                           │
   │   Odds: 1.45                            │
   │   BetMode: Sportsbook                   │
   │   Status: Pending                       │
   │ Save to database                        │
   └─────────────────────────────────────────┘
                    │
                    │ Later: Game ends
                    ▼
10. ADMIN: Settlement
   ┌─────────────────────────────────────────┐
   │ POST /api/admin/settlement/event/guid-1 │
   │ {                                       │
   │   homeScore: 24,  (Chiefs win!)         │
   │   awayScore: 17                         │
   │ }                                       │
   │                                         │
   │ → Settle all bets                       │
   │ → Pay winners ($145 to user)            │
   │ → Track revenue (house profit/loss)     │
   └─────────────────────────────────────────┘
```

---

## Integration Points

### Shared Components

```
┌─────────────────────────────────────────────────────────────────┐
│                    SHARED BETWEEN PROJECTS                      │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  SportsBetting.Domain (Entities)                         │  │
│  │  - Event, Market, Outcome                                │  │
│  │  - Sport, League, Team                                   │  │
│  │  - Bet, ExchangeBet, BetMatch                            │  │
│  │  - User, Wallet, UserStatistics                          │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              ▲                                  │
│                              │                                  │
│  ┌──────────────────────────┼───────────────────────────────┐  │
│  │  SportsBetting.Data (DbContext)                          │  │
│  │  - SportsBettingDbContext                                │  │
│  │  - Entity configurations                                 │  │
│  │  - Migrations                                            │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              ▲                                  │
│                              │                                  │
│                    ┌─────────┴─────────┐                       │
│                    │                   │                       │
│         ┌──────────┴────────┐  ┌──────┴─────────────┐         │
│         │ SportsBetting.API │  │ Listener.Worker    │         │
│         │                   │  │                    │         │
│         │ - Controllers     │  │ - OddsApiClient    │         │
│         │ - Services        │  │ - Mappers          │         │
│         │ - SignalR         │  │ - SyncService      │         │
│         └───────────────────┘  └────────────────────┘         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### New Database Columns

> ✅ **SCHEMA CHANGES COMPLETE** (2025-11-15)
> Migration `AddOddsApiExternalIds` applied successfully
> See: `/SportsBetting/ODDS_API_SCHEMA_CHANGES_COMPLETE.md`

```
Events table (existing + new columns):
┌────────────────┬──────────────┬─────────────────────┐
│ Column Name    │ Type         │ Description         │
├────────────────┼──────────────┼─────────────────────┤
│ Id             │ Guid         │ Primary key         │
│ HomeTeamId     │ Guid         │ (existing)          │
│ AwayTeamId     │ Guid         │ (existing)          │
│ Status         │ Enum         │ (existing)          │
│ StartTime      │ DateTime     │ (existing)          │
│ ExternalId     │ VARCHAR(255) │ ✅ ADDED - from API │
│ LastSyncedAt   │ DateTime     │ ✅ ADDED - sync ts  │
└────────────────┴──────────────┴─────────────────────┘

Markets table (existing + new columns):
┌────────────────┬──────────────┬─────────────────────┐
│ Column Name    │ Type         │ Description         │
├────────────────┼──────────────┼─────────────────────┤
│ Id             │ Guid         │ Primary key         │
│ EventId        │ Guid         │ (existing)          │
│ Type           │ Enum         │ (existing)          │
│ ExternalId     │ VARCHAR(255) │ ✅ ADDED - API key  │
└────────────────┴──────────────┴─────────────────────┘

Outcomes table (existing + new columns):
┌────────────────┬──────────────┬─────────────────────┐
│ Column Name    │ Type         │ Description         │
├────────────────┼──────────────┼─────────────────────┤
│ Id             │ Guid         │ Primary key         │
│ MarketId       │ Guid         │ (existing)          │
│ Description    │ VARCHAR(200) │ (existing)          │
│ CurrentOdds    │ DECIMAL(10,4)│ (existing)          │
│ ExternalId     │ VARCHAR(255) │ ✅ ADDED - from API │
│ LastOddsUpdate │ DateTime     │ ✅ ADDED - odds ts  │
└────────────────┴──────────────┴─────────────────────┘
```

---

## Deployment Architecture

```
PRODUCTION ENVIRONMENT:

┌─────────────────────────────────────────────────────────────────┐
│                         CLOUD INFRASTRUCTURE                    │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Container Registry (Docker Hub / Azure ACR)             │  │
│  │  - sportsbetting-api:latest                              │  │
│  │  - sportsbetting-listener:latest                         │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
│                              │ Pull images                      │
│                              ▼                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Container Orchestrator (Docker Compose / Kubernetes)    │  │
│  │                                                          │  │
│  │  ┌─────────────────┐     ┌─────────────────┐           │  │
│  │  │ API Container   │     │ Listener        │           │  │
│  │  │ (3 replicas)    │     │ Container       │           │  │
│  │  │ - Port 5192     │     │ (1 replica)     │           │  │
│  │  │ - Auto-scale    │     │ - Background    │           │  │
│  │  └─────────────────┘     └─────────────────┘           │  │
│  │          │                        │                      │  │
│  └──────────┼────────────────────────┼──────────────────────┘  │
│             │                        │                         │
│             └────────────┬───────────┘                         │
│                          │ Both connect to DB                  │
│  ┌───────────────────────┼──────────────────────────────────┐  │
│  │  PostgreSQL Database  │                                  │  │
│  │  - Managed service (Azure DB, AWS RDS)                   │  │
│  │  - Automatic backups                                     │  │
│  │  - High availability                                     │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Load Balancer                                           │  │
│  │  - Routes traffic to API containers                      │  │
│  │  - SSL termination                                       │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
└──────────────────────────────┼──────────────────────────────────┘
                               │
                               │ HTTPS
                               ▼
                         [Users/Clients]
```

---

This diagram provides a complete visual overview of:
1. How The Odds API integrates with your system
2. Data flow from external API to user bets
3. Shared components between projects
4. Database schema changes
5. Production deployment architecture
