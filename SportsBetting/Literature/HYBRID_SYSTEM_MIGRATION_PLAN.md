# Hybrid Sportsbook/Exchange Migration Plan

**Purpose**: Step-by-step guide to refactor the current sportsbook system to support both traditional betting and P2P exchange betting simultaneously.

**Audience**: Development team implementing the hybrid system
**Status**: Planning Document
**Estimated Effort**: 10-15 weeks

---

## Table of Contents

1. [Migration Strategy](#migration-strategy)
2. [Phase 0: Pre-Migration Preparation](#phase-0-pre-migration-preparation)
3. [Phase 1: Database Schema Changes](#phase-1-database-schema-changes)
4. [Phase 2: Domain Model Refactoring](#phase-2-domain-model-refactoring)
5. [Phase 3: Service Layer Updates](#phase-3-service-layer-updates)
6. [Phase 4: API Layer Changes](#phase-4-api-layer-changes)
7. [Phase 5: Settlement Logic Dual-Mode](#phase-5-settlement-logic-dual-mode)
8. [Phase 6: Testing & Validation](#phase-6-testing-validation)
9. [Phase 7: Deployment Strategy](#phase-7-deployment-strategy)
10. [Rollback Plan](#rollback-plan)

---

## Migration Strategy

### Core Principles

1. **Zero Downtime**: All changes must be backward compatible
2. **Feature Flags**: Use flags to enable/disable exchange features
3. **Gradual Rollout**: Start with one market, expand incrementally
4. **Data Integrity**: Existing bets must continue working
5. **Rollback Ready**: Every phase can be reverted

### Feature Flags Configuration

```csharp
// appsettings.json
{
  "FeatureFlags": {
    "EnableExchange": false,           // Master switch
    "EnableHybridMode": false,         // Show both options to users
    "ExchangeMarketsWhitelist": [],    // Specific markets enabled for exchange
    "ConsensusOddsValidation": false,  // Odds validation
    "AutoMatching": true,              // Auto-match on placement
    "AllowPartialMatching": true       // Partial fills
  }
}
```

---

## Phase 0: Pre-Migration Preparation

**Duration**: 1 week
**Risk**: Low

### Step 1: Create Feature Branch

```bash
git checkout -b feature/hybrid-betting-system
git push -u origin feature/hybrid-betting-system
```

### Step 2: Add Feature Flag Infrastructure

**File**: `SportsBetting.Domain/Configuration/FeatureFlags.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Configuration;

public class FeatureFlags
{
    public bool EnableExchange { get; set; }
    public bool EnableHybridMode { get; set; }
    public List<Guid> ExchangeMarketsWhitelist { get; set; } = new();
    public bool ConsensusOddsValidation { get; set; }
    public bool AutoMatching { get; set; } = true;
    public bool AllowPartialMatching { get; set; } = true;
    public decimal DefaultCommissionRate { get; set; } = 0.02m; // 2%
}
```

**File**: `SportsBetting.API/Program.cs` (MODIFY)

```csharp
// Add after builder.Services configuration
builder.Services.Configure<FeatureFlags>(
    builder.Configuration.GetSection("FeatureFlags"));
```

### Step 3: Database Backup Strategy

```bash
# Create backup before any migrations
pg_dump -U calebwilliams sportsbetting > backup_pre_hybrid_$(date +%Y%m%d).sql

# Verify backup
ls -lh backup_pre_hybrid_*.sql
```

### Step 4: Set Up Test Environment

```bash
# Create separate test database for hybrid features
createdb -U calebwilliams sportsbetting_hybrid_test

# Copy production data
pg_dump -U calebwilliams sportsbetting | \
  psql -U calebwilliams sportsbetting_hybrid_test
```

---

## Phase 1: Database Schema Changes

**Duration**: 1 week
**Risk**: Medium (schema changes)

### Migration 1: Add BetMode and Market Mode

**File**: `SportsBetting.Data/Migrations/YYYYMMDDHHMMSS_AddHybridSupport.cs` (NEW)

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddHybridSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Add BetMode to Bets table (defaults to Sportsbook)
        migrationBuilder.AddColumn<string>(
            name: "BetMode",
            table: "Bets",
            type: "varchar(20)",
            nullable: false,
            defaultValue: "Sportsbook");

        // 2. Add State to Bets table (for exchange bets)
        migrationBuilder.AddColumn<string>(
            name: "State",
            table: "Bets",
            type: "varchar(20)",
            nullable: false,
            defaultValue: "Pending");

        // 3. Add ProposedOdds to Bets (for exchange bets)
        migrationBuilder.AddColumn<decimal>(
            name: "ProposedOdds",
            table: "Bets",
            type: "decimal(18,2)",
            nullable: true);

        // 4. Add Market mode
        migrationBuilder.AddColumn<string>(
            name: "Mode",
            table: "Markets",
            type: "varchar(20)",
            nullable: false,
            defaultValue: "Sportsbook");

        // 5. Add exchange commission rate
        migrationBuilder.AddColumn<decimal>(
            name: "ExchangeCommissionRate",
            table: "Markets",
            type: "decimal(5,4)",
            nullable: true);

        // 6. Create indexes for performance
        migrationBuilder.CreateIndex(
            name: "IX_Bets_State_BetMode",
            table: "Bets",
            columns: new[] { "State", "BetMode" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BetMode", table: "Bets");
        migrationBuilder.DropColumn(name: "State", table: "Bets");
        migrationBuilder.DropColumn(name: "ProposedOdds", table: "Bets");
        migrationBuilder.DropColumn(name: "Mode", table: "Markets");
        migrationBuilder.DropColumn(name: "ExchangeCommissionRate", table: "Markets");
        migrationBuilder.DropIndex(name: "IX_Bets_State_BetMode", table: "Bets");
    }
}
```

### Migration 2: Create Exchange Tables

**File**: `SportsBetting.Data/Migrations/YYYYMMDDHHMMSS_CreateExchangeTables.cs` (NEW)

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class CreateExchangeTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Create ExchangeBets table
        migrationBuilder.CreateTable(
            name: "ExchangeBets",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                BetId = table.Column<Guid>(nullable: false),
                Side = table.Column<string>(type: "varchar(10)", nullable: false),
                ProposedOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalStake = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                MatchedStake = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0),
                UnmatchedStake = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                State = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Unmatched"),
                CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                CancelledAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExchangeBets", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExchangeBets_Bets_BetId",
                    column: x => x.BetId,
                    principalTable: "Bets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.CheckConstraint(
                    "CK_ExchangeBets_Side",
                    "\"Side\" IN ('Back', 'Lay')");
                table.CheckConstraint(
                    "CK_ExchangeBets_MatchedStake",
                    "\"MatchedStake\" >= 0 AND \"MatchedStake\" <= \"TotalStake\"");
            });

        // 2. Create BetMatches table
        migrationBuilder.CreateTable(
            name: "BetMatches",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                BackBetId = table.Column<Guid>(nullable: false),
                LayBetId = table.Column<Guid>(nullable: false),
                MatchedStake = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                MatchedOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                MatchedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()"),
                IsSettled = table.Column<bool>(nullable: false, defaultValue: false),
                WinnerBetId = table.Column<Guid>(nullable: true),
                SettledAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BetMatches", x => x.Id);
                table.ForeignKey(
                    name: "FK_BetMatches_ExchangeBets_BackBetId",
                    column: x => x.BackBetId,
                    principalTable: "ExchangeBets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BetMatches_ExchangeBets_LayBetId",
                    column: x => x.LayBetId,
                    principalTable: "ExchangeBets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_BetMatches_Bets_WinnerBetId",
                    column: x => x.WinnerBetId,
                    principalTable: "Bets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        // 3. Create ConsensusOdds table
        migrationBuilder.CreateTable(
            name: "ConsensusOdds",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                OutcomeId = table.Column<Guid>(nullable: false),
                AverageOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                MinOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                MaxOdds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                SampleSize = table.Column<int>(nullable: false),
                Source = table.Column<string>(type: "varchar(50)", nullable: false),
                FetchedAt = table.Column<DateTime>(nullable: false),
                ExpiresAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConsensusOdds", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConsensusOdds_Outcomes_OutcomeId",
                    column: x => x.OutcomeId,
                    principalTable: "Outcomes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // 4. Create indexes for query performance
        migrationBuilder.CreateIndex(
            name: "IX_ExchangeBets_State_Outcome",
            table: "ExchangeBets",
            columns: new[] { "State" },
            filter: "\"State\" IN ('Unmatched', 'PartiallyMatched')");

        migrationBuilder.CreateIndex(
            name: "IX_ExchangeBets_BetId",
            table: "ExchangeBets",
            column: "BetId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_BetMatches_BackBetId",
            table: "BetMatches",
            column: "BackBetId");

        migrationBuilder.CreateIndex(
            name: "IX_BetMatches_LayBetId",
            table: "BetMatches",
            column: "LayBetId");

        migrationBuilder.CreateIndex(
            name: "IX_BetMatches_IsSettled",
            table: "BetMatches",
            column: "IsSettled",
            filter: "\"IsSettled\" = false");

        migrationBuilder.CreateIndex(
            name: "IX_ConsensusOdds_Outcome_Expiry",
            table: "ConsensusOdds",
            columns: new[] { "OutcomeId", "ExpiresAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BetMatches");
        migrationBuilder.DropTable(name: "ExchangeBets");
        migrationBuilder.DropTable(name: "ConsensusOdds");
    }
}
```

### Apply Migrations

```bash
cd SportsBetting.Data
dotnet ef migrations add AddHybridSupport
dotnet ef migrations add CreateExchangeTables

# Test on test database first
dotnet ef database update --connection "Host=localhost;Database=sportsbetting_hybrid_test;Username=calebwilliams"

# If successful, apply to main database
dotnet ef database update
```

---

## Phase 2: Domain Model Refactoring

**Duration**: 1 week
**Risk**: Medium

### Step 1: Add Enums

**File**: `SportsBetting.Domain/Enums/BetMode.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Enums;

public enum BetMode
{
    Sportsbook = 0,  // Traditional: bet against house
    Exchange = 1      // P2P: bet against other users
}
```

**File**: `SportsBetting.Domain/Enums/BetSide.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Enums;

public enum BetSide
{
    Back = 0,  // Betting FOR an outcome
    Lay = 1    // Betting AGAINST an outcome
}
```

**File**: `SportsBetting.Domain/Enums/BetState.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Enums;

public enum BetState
{
    // Sportsbook states
    Pending = 0,
    Won = 1,
    Lost = 2,
    Pushed = 3,
    Settled = 4,

    // Exchange states
    Unmatched = 10,
    PartiallyMatched = 11,
    Matched = 12,
    Cancelled = 13
}
```

**File**: `SportsBetting.Domain/Enums/MarketMode.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Enums;

public enum MarketMode
{
    Sportsbook = 0,  // House sets odds
    Exchange = 1,    // User-proposed odds
    Hybrid = 2       // Both available
}
```

### Step 2: Modify Existing Entities

**File**: `SportsBetting.Domain/Entities/Bet.cs` (MODIFY)

Add these properties:

```csharp
public class Bet
{
    // ... existing properties ...

    // NEW: Hybrid system support
    public BetMode BetMode { get; private set; } = BetMode.Sportsbook;
    public BetState State { get; private set; } = BetState.Pending;
    public decimal? ProposedOdds { get; private set; }  // For exchange bets

    // NEW: Constructor for exchange bets
    public static Bet CreateExchangeSingle(
        User user,
        Money stake,
        Event evt,
        Market market,
        Outcome outcome,
        decimal proposedOdds,
        BetSide side)
    {
        if (market.Mode == MarketMode.Sportsbook)
            throw new InvalidOperationException("Cannot create exchange bet on sportsbook market");

        var bet = new Bet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TicketNumber = GenerateTicketNumber(),
            Type = BetType.Single,
            BetMode = BetMode.Exchange,
            State = BetState.Unmatched,
            Stake = stake,
            ProposedOdds = proposedOdds,
            CombinedOdds = new Odds(proposedOdds),
            PotentialPayout = stake * proposedOdds,
            PlacedAt = DateTime.UtcNow,
            _selections = new List<BetSelection>()
        };

        var selection = new BetSelection(
            evt.Id,
            evt.Name,
            market.Id,
            market.Type,
            market.Name,
            outcome.Id,
            outcome.Name,
            new Odds(proposedOdds),
            outcome.Line
        );

        bet._selections.Add(selection);
        return bet;
    }

    // NEW: Method to transition states for exchange bets
    public void MarkAsMatched()
    {
        if (BetMode != BetMode.Exchange)
            throw new InvalidOperationException("Only exchange bets can be matched");

        if (State != BetState.Unmatched && State != BetState.PartiallyMatched)
            throw new InvalidOperationException($"Cannot match bet in {State} state");

        State = BetState.Matched;
    }

    public void MarkAsPartiallyMatched()
    {
        if (BetMode != BetMode.Exchange)
            throw new InvalidOperationException("Only exchange bets can be partially matched");

        State = BetState.PartiallyMatched;
    }

    public void CancelUnmatched()
    {
        if (BetMode != BetMode.Exchange)
            throw new InvalidOperationException("Only exchange bets can be cancelled");

        if (State == BetState.Matched)
            throw new InvalidOperationException("Cannot cancel matched bet");

        State = BetState.Cancelled;
    }

    // ... rest of existing code ...
}
```

**File**: `SportsBetting.Domain/Entities/Market.cs` (MODIFY)

Add these properties:

```csharp
public class Market
{
    // ... existing properties ...

    // NEW: Hybrid system support
    public MarketMode Mode { get; private set; } = MarketMode.Sportsbook;
    public decimal? ExchangeCommissionRate { get; private set; }

    // NEW: Set market mode (admin only)
    public void SetMode(MarketMode mode, decimal? exchangeCommissionRate = null)
    {
        Mode = mode;

        if (mode == MarketMode.Exchange || mode == MarketMode.Hybrid)
        {
            ExchangeCommissionRate = exchangeCommissionRate ?? 0.02m;
        }
    }

    // ... rest of existing code ...
}
```

### Step 3: Create New Entities

**File**: `SportsBetting.Domain/Entities/ExchangeBet.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Entities;

public class ExchangeBet
{
    public Guid Id { get; private set; }
    public Guid BetId { get; private set; }
    public Bet Bet { get; private set; } = null!;

    public BetSide Side { get; private set; }
    public decimal ProposedOdds { get; private set; }
    public decimal TotalStake { get; private set; }
    public decimal MatchedStake { get; private set; }
    public decimal UnmatchedStake { get; private set; }

    public BetState State { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // Navigation
    private readonly List<BetMatch> _matchesAsBack = new();
    private readonly List<BetMatch> _matchesAsLay = new();
    public IReadOnlyList<BetMatch> MatchesAsBack => _matchesAsBack.AsReadOnly();
    public IReadOnlyList<BetMatch> MatchesAsLay => _matchesAsLay.AsReadOnly();

    // EF Constructor
    private ExchangeBet() { }

    public ExchangeBet(Bet bet, BetSide side, decimal proposedOdds, decimal totalStake)
    {
        if (bet.BetMode != BetMode.Exchange)
            throw new InvalidOperationException("Can only create ExchangeBet for exchange-mode bets");

        Id = Guid.NewGuid();
        BetId = bet.Id;
        Bet = bet;
        Side = side;
        ProposedOdds = proposedOdds;
        TotalStake = totalStake;
        MatchedStake = 0;
        UnmatchedStake = totalStake;
        State = BetState.Unmatched;
        CreatedAt = DateTime.UtcNow;
    }

    public void ApplyMatch(decimal matchedAmount)
    {
        if (matchedAmount > UnmatchedStake)
            throw new InvalidOperationException("Cannot match more than unmatched stake");

        MatchedStake += matchedAmount;
        UnmatchedStake -= matchedAmount;

        State = UnmatchedStake > 0 ? BetState.PartiallyMatched : BetState.Matched;

        // Update parent bet state
        if (State == BetState.Matched)
            Bet.MarkAsMatched();
        else
            Bet.MarkAsPartiallyMatched();
    }

    public void Cancel()
    {
        if (State == BetState.Matched)
            throw new InvalidOperationException("Cannot cancel fully matched bet");

        State = BetState.Cancelled;
        CancelledAt = DateTime.UtcNow;
        Bet.CancelUnmatched();
    }

    public decimal CalculateLiability()
    {
        // For Lay bets, liability is what you can lose
        return Side == BetSide.Lay
            ? TotalStake * (ProposedOdds - 1)
            : TotalStake;
    }
}
```

**File**: `SportsBetting.Domain/Entities/BetMatch.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Entities;

public class BetMatch
{
    public Guid Id { get; private set; }

    public Guid BackBetId { get; private set; }
    public ExchangeBet BackBet { get; private set; } = null!;

    public Guid LayBetId { get; private set; }
    public ExchangeBet LayBet { get; private set; } = null!;

    public decimal MatchedStake { get; private set; }
    public decimal MatchedOdds { get; private set; }
    public DateTime MatchedAt { get; private set; }

    public bool IsSettled { get; private set; }
    public Guid? WinnerBetId { get; private set; }
    public DateTime? SettledAt { get; private set; }

    // EF Constructor
    private BetMatch() { }

    public BetMatch(
        ExchangeBet backBet,
        ExchangeBet layBet,
        decimal matchedStake,
        decimal matchedOdds)
    {
        if (backBet.Side != BetSide.Back)
            throw new ArgumentException("First bet must be a back bet");
        if (layBet.Side != BetSide.Lay)
            throw new ArgumentException("Second bet must be a lay bet");

        Id = Guid.NewGuid();
        BackBetId = backBet.Id;
        BackBet = backBet;
        LayBetId = layBet.Id;
        LayBet = layBet;
        MatchedStake = matchedStake;
        MatchedOdds = matchedOdds;
        MatchedAt = DateTime.UtcNow;
        IsSettled = false;
    }

    public void Settle(bool backBetWins)
    {
        if (IsSettled)
            throw new InvalidOperationException("Match already settled");

        WinnerBetId = backBetWins ? BackBet.BetId : LayBet.BetId;
        IsSettled = true;
        SettledAt = DateTime.UtcNow;
    }

    public decimal CalculateWinnings(decimal commissionRate)
    {
        var grossWinnings = MatchedStake * (MatchedOdds - 1);
        var commission = grossWinnings * commissionRate;
        return grossWinnings - commission;
    }
}
```

**File**: `SportsBetting.Domain/Entities/ConsensusOdds.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Entities;

public class ConsensusOdds
{
    public Guid Id { get; private set; }
    public Guid OutcomeId { get; private set; }
    public Outcome Outcome { get; private set; } = null!;

    public decimal AverageOdds { get; private set; }
    public decimal MinOdds { get; private set; }
    public decimal MaxOdds { get; private set; }
    public int SampleSize { get; private set; }

    public string Source { get; private set; }
    public DateTime FetchedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // EF Constructor
    private ConsensusOdds()
    {
        Source = string.Empty;
    }

    public ConsensusOdds(
        Guid outcomeId,
        decimal averageOdds,
        decimal minOdds,
        decimal maxOdds,
        int sampleSize,
        string source,
        TimeSpan ttl)
    {
        Id = Guid.NewGuid();
        OutcomeId = outcomeId;
        AverageOdds = averageOdds;
        MinOdds = minOdds;
        MaxOdds = maxOdds;
        SampleSize = sampleSize;
        Source = source;
        FetchedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.Add(ttl);
    }

    public bool IsExpired() => DateTime.UtcNow >= ExpiresAt;

    public decimal CalculateDeviation(decimal proposedOdds)
    {
        return Math.Abs(proposedOdds - AverageOdds) / AverageOdds * 100;
    }
}
```

### Step 4: Update DbContext

**File**: `SportsBetting.Data/SportsBettingDbContext.cs` (MODIFY)

Add DbSets:

```csharp
public class SportsBettingDbContext : DbContext
{
    // ... existing DbSets ...

    // NEW: Exchange tables
    public DbSet<ExchangeBet> ExchangeBets { get; set; }
    public DbSet<BetMatch> BetMatches { get; set; }
    public DbSet<ConsensusOdds> ConsensusOdds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ... existing configurations ...

        // NEW: Configure ExchangeBet
        modelBuilder.Entity<ExchangeBet>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Bet)
                  .WithOne()
                  .HasForeignKey<ExchangeBet>(e => e.BetId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Side)
                  .HasConversion<string>()
                  .HasMaxLength(10);

            entity.Property(e => e.State)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(e => e.ProposedOdds)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.TotalStake)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.MatchedStake)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.UnmatchedStake)
                  .HasColumnType("decimal(18,2)");
        });

        // NEW: Configure BetMatch
        modelBuilder.Entity<BetMatch>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.BackBet)
                  .WithMany(b => b.MatchesAsBack)
                  .HasForeignKey(e => e.BackBetId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.LayBet)
                  .WithMany(b => b.MatchesAsLay)
                  .HasForeignKey(e => e.LayBetId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.MatchedStake)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.MatchedOdds)
                  .HasColumnType("decimal(18,2)");
        });

        // NEW: Configure ConsensusOdds
        modelBuilder.Entity<ConsensusOdds>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Outcome)
                  .WithMany()
                  .HasForeignKey(e => e.OutcomeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.AverageOdds)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.MinOdds)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.MaxOdds)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Source)
                  .HasMaxLength(50);
        });
    }
}
```

---

## Phase 3: Service Layer Updates

**Duration**: 2 weeks
**Risk**: Medium

### Step 1: Create Bet Matching Service

**File**: `SportsBetting.Domain/Services/IBetMatchingService.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Services;

public interface IBetMatchingService
{
    /// <summary>
    /// Try to match a new exchange bet with existing unmatched bets
    /// </summary>
    Task<MatchResult> MatchBet(ExchangeBet exchangeBet);

    /// <summary>
    /// Get all unmatched bets for an outcome
    /// </summary>
    Task<List<ExchangeBet>> GetUnmatchedBets(
        Guid outcomeId,
        BetSide? side = null,
        int limit = 50);

    /// <summary>
    /// Match a specific unmatched bet (user taking someone's bet)
    /// </summary>
    Task<MatchResult> TakeBet(Guid exchangeBetId, Guid userId, decimal stakeToMatch);

    /// <summary>
    /// Cancel an unmatched or partially matched bet
    /// </summary>
    Task CancelBet(Guid exchangeBetId, Guid userId);
}

public class MatchResult
{
    public bool FullyMatched { get; set; }
    public decimal MatchedAmount { get; set; }
    public decimal UnmatchedAmount { get; set; }
    public List<BetMatch> Matches { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
```

**File**: `SportsBetting.Domain/Services/BetMatchingService.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Services;

public class BetMatchingService : IBetMatchingService
{
    private readonly SportsBettingDbContext _context;
    private readonly ILogger<BetMatchingService> _logger;

    public BetMatchingService(
        SportsBettingDbContext context,
        ILogger<BetMatchingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MatchResult> MatchBet(ExchangeBet newBet)
    {
        var result = new MatchResult
        {
            MatchedAmount = 0,
            UnmatchedAmount = newBet.TotalStake
        };

        // Get outcome to find opposing bets
        var outcomeId = newBet.Bet.Selections.First().OutcomeId;

        // Find opposing bets
        var oppositeSide = newBet.Side == BetSide.Back ? BetSide.Lay : BetSide.Back;

        var candidates = await GetMatchingCandidates(
            outcomeId,
            oppositeSide,
            newBet.ProposedOdds,
            newBet.Side);

        decimal remainingStake = newBet.TotalStake;

        foreach (var candidate in candidates)
        {
            if (remainingStake <= 0)
                break;

            // Calculate how much we can match
            decimal matchAmount = Math.Min(remainingStake, candidate.UnmatchedStake);

            // Create the match
            var match = new BetMatch(
                backBet: newBet.Side == BetSide.Back ? newBet : candidate,
                layBet: newBet.Side == BetSide.Lay ? newBet : candidate,
                matchedStake: matchAmount,
                matchedOdds: newBet.ProposedOdds
            );

            _context.BetMatches.Add(match);

            // Update both bets
            newBet.ApplyMatch(matchAmount);
            candidate.ApplyMatch(matchAmount);

            remainingStake -= matchAmount;
            result.Matches.Add(match);

            _logger.LogInformation(
                "Matched {Amount} between {NewBetId} and {CandidateId} at {Odds}",
                matchAmount, newBet.Id, candidate.Id, newBet.ProposedOdds);
        }

        result.MatchedAmount = newBet.TotalStake - remainingStake;
        result.UnmatchedAmount = remainingStake;
        result.FullyMatched = remainingStake == 0;
        result.Message = result.FullyMatched
            ? "Bet fully matched"
            : $"Bet partially matched: {result.MatchedAmount:C} matched, {result.UnmatchedAmount:C} unmatched";

        await _context.SaveChangesAsync();

        return result;
    }

    private async Task<List<ExchangeBet>> GetMatchingCandidates(
        Guid outcomeId,
        BetSide oppositeSide,
        decimal proposedOdds,
        BetSide originalSide)
    {
        var query = _context.ExchangeBets
            .Include(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
            .Where(eb => eb.Bet.Selections.Any(s => s.OutcomeId == outcomeId))
            .Where(eb => eb.Side == oppositeSide)
            .Where(eb => eb.State == BetState.Unmatched ||
                         eb.State == BetState.PartiallyMatched);

        // Filter by odds compatibility
        if (originalSide == BetSide.Back)
        {
            // Back bet: need Lay bets with odds <= our proposed odds
            query = query.Where(eb => eb.ProposedOdds <= proposedOdds);
        }
        else
        {
            // Lay bet: need Back bets with odds >= our proposed odds
            query = query.Where(eb => eb.ProposedOdds >= proposedOdds);
        }

        // Sort: best odds first, then earliest timestamp (FIFO)
        query = originalSide == BetSide.Back
            ? query.OrderBy(eb => eb.ProposedOdds).ThenBy(eb => eb.CreatedAt)
            : query.OrderByDescending(eb => eb.ProposedOdds).ThenBy(eb => eb.CreatedAt);

        return await query.ToListAsync();
    }

    public async Task<List<ExchangeBet>> GetUnmatchedBets(
        Guid outcomeId,
        BetSide? side = null,
        int limit = 50)
    {
        var query = _context.ExchangeBets
            .Include(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
            .Where(eb => eb.Bet.Selections.Any(s => s.OutcomeId == outcomeId))
            .Where(eb => eb.State == BetState.Unmatched ||
                         eb.State == BetState.PartiallyMatched);

        if (side.HasValue)
        {
            query = query.Where(eb => eb.Side == side.Value);
        }

        return await query
            .OrderByDescending(eb => eb.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<MatchResult> TakeBet(
        Guid exchangeBetId,
        Guid userId,
        decimal stakeToMatch)
    {
        var targetBet = await _context.ExchangeBets
            .Include(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
            .FirstOrDefaultAsync(eb => eb.Id == exchangeBetId);

        if (targetBet == null)
            throw new ArgumentException("Exchange bet not found");

        if (targetBet.Bet.UserId == userId)
            throw new InvalidOperationException("Cannot match your own bet");

        if (stakeToMatch > targetBet.UnmatchedStake)
            throw new InvalidOperationException("Stake exceeds available unmatched amount");

        // Create opposing bet for the user
        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new ArgumentException("User not found");

        var oppositeSide = targetBet.Side == BetSide.Back ? BetSide.Lay : BetSide.Back;
        var outcome = await _context.Outcomes.FindAsync(
            targetBet.Bet.Selections.First().OutcomeId);

        if (outcome == null)
            throw new InvalidOperationException("Outcome not found");

        var event = await _context.Events
            .Include(e => e.Markets)
            .FirstOrDefaultAsync(e => e.Markets.Any(m => m.Outcomes.Contains(outcome)));

        var market = event?.Markets.FirstOrDefault(m => m.Outcomes.Contains(outcome));

        // Create counter bet
        var counterBet = Bet.CreateExchangeSingle(
            user,
            new Money(stakeToMatch, user.Wallet!.Balance.Currency),
            event!,
            market!,
            outcome,
            targetBet.ProposedOdds,
            oppositeSide);

        var counterExchangeBet = new ExchangeBet(
            counterBet,
            oppositeSide,
            targetBet.ProposedOdds,
            stakeToMatch);

        _context.Bets.Add(counterBet);
        _context.ExchangeBets.Add(counterExchangeBet);

        // Create match
        var match = new BetMatch(
            backBet: targetBet.Side == BetSide.Back ? targetBet : counterExchangeBet,
            layBet: targetBet.Side == BetSide.Lay ? targetBet : counterExchangeBet,
            matchedStake: stakeToMatch,
            matchedOdds: targetBet.ProposedOdds);

        _context.BetMatches.Add(match);

        // Update states
        targetBet.ApplyMatch(stakeToMatch);
        counterExchangeBet.ApplyMatch(stakeToMatch);

        await _context.SaveChangesAsync();

        return new MatchResult
        {
            FullyMatched = true,
            MatchedAmount = stakeToMatch,
            UnmatchedAmount = 0,
            Matches = new List<BetMatch> { match },
            Message = "Successfully matched bet"
        };
    }

    public async Task CancelBet(Guid exchangeBetId, Guid userId)
    {
        var bet = await _context.ExchangeBets
            .Include(eb => eb.Bet)
            .FirstOrDefaultAsync(eb => eb.Id == exchangeBetId);

        if (bet == null)
            throw new ArgumentException("Exchange bet not found");

        if (bet.Bet.UserId != userId)
            throw new UnauthorizedAccessException("Cannot cancel another user's bet");

        if (bet.State == BetState.Matched)
            throw new InvalidOperationException("Cannot cancel fully matched bet");

        bet.Cancel();
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} cancelled exchange bet {BetId}. Unmatched stake: {UnmatchedStake}",
            userId, exchangeBetId, bet.UnmatchedStake);
    }
}
```

### Step 2: Create Odds Validation Service

**File**: `SportsBetting.Domain/Services/IOddsValidationService.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Services;

public interface IOddsValidationService
{
    Task<ValidationResult> ValidateOdds(
        Guid outcomeId,
        decimal proposedOdds,
        decimal tolerancePercent = 10.0m);

    Task<ConsensusOdds?> GetConsensusOdds(Guid outcomeId);

    Task RefreshConsensusOdds(Guid eventId);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public decimal ConsensusOdds { get; set; }
    public decimal ProposedOdds { get; set; }
    public decimal DeviationPercent { get; set; }
    public string Reason { get; set; } = string.Empty;
}
```

**File**: `SportsBetting.Domain/Services/OddsValidationService.cs` (NEW)

```csharp
namespace SportsBetting.Domain.Services;

public class OddsValidationService : IOddsValidationService
{
    private readonly SportsBettingDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OddsValidationService> _logger;

    public OddsValidationService(
        SportsBettingDbContext context,
        IMemoryCache cache,
        ILogger<OddsValidationService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateOdds(
        Guid outcomeId,
        decimal proposedOdds,
        decimal tolerancePercent = 10.0m)
    {
        var consensus = await GetConsensusOdds(outcomeId);

        if (consensus == null || consensus.IsExpired())
        {
            _logger.LogWarning(
                "No valid consensus odds for outcome {OutcomeId}", outcomeId);

            return new ValidationResult
            {
                IsValid = true, // Allow with warning
                ProposedOdds = proposedOdds,
                Reason = "No consensus data available - proceeding with caution"
            };
        }

        var deviation = consensus.CalculateDeviation(proposedOdds);

        return new ValidationResult
        {
            IsValid = deviation <= tolerancePercent,
            ConsensusOdds = consensus.AverageOdds,
            ProposedOdds = proposedOdds,
            DeviationPercent = deviation,
            Reason = deviation > tolerancePercent
                ? $"Odds deviate {deviation:F1}% from market consensus (max {tolerancePercent}%)"
                : "Odds within acceptable range"
        };
    }

    public async Task<ConsensusOdds?> GetConsensusOdds(Guid outcomeId)
    {
        var cacheKey = $"consensus_odds_{outcomeId}";

        if (_cache.TryGetValue(cacheKey, out ConsensusOdds? cached))
        {
            return cached;
        }

        var consensus = await _context.ConsensusOdds
            .Where(co => co.OutcomeId == outcomeId)
            .Where(co => co.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(co => co.FetchedAt)
            .FirstOrDefaultAsync();

        if (consensus != null)
        {
            var ttl = consensus.ExpiresAt - DateTime.UtcNow;
            _cache.Set(cacheKey, consensus, ttl);
        }

        return consensus;
    }

    public async Task RefreshConsensusOdds(Guid eventId)
    {
        // TODO: Implement The Odds API integration
        // For now, placeholder that returns mock data
        _logger.LogInformation(
            "Refreshing consensus odds for event {EventId}", eventId);

        throw new NotImplementedException("The Odds API integration pending");
    }
}
```

### Step 3: Update Settlement Service

**File**: `SportsBetting.Domain/Services/ISettlementService.cs` (MODIFY)

```csharp
public interface ISettlementService
{
    // Existing method
    void SettleBet(Bet bet, List<Event> events);

    // NEW: Settle exchange match
    Task SettleExchangeMatch(BetMatch match, Event evt, decimal commissionRate);

    // NEW: Settle all matches for an event
    Task SettleAllExchangeMatches(Guid eventId);
}
```

**File**: `SportsBetting.Domain/Services/SettlementService.cs` (MODIFY)

Add this method:

```csharp
public async Task SettleExchangeMatch(
    BetMatch match,
    Event evt,
    decimal commissionRate)
{
    if (match.IsSettled)
    {
        _logger.LogWarning("Match {MatchId} already settled", match.Id);
        return;
    }

    // Determine outcome
    var outcome = evt.Markets
        .SelectMany(m => m.Outcomes)
        .FirstOrDefault(o => o.Id == match.BackBet.Bet.Selections.First().OutcomeId);

    if (outcome == null)
    {
        throw new InvalidOperationException("Outcome not found for match");
    }

    bool backBetWins = outcome.IsWinner;

    // Get winner and loser
    var winnerBet = backBetWins ? match.BackBet : match.LayBet;
    var loserBet = backBetWins ? match.LayBet : match.BackBet;

    // Calculate payouts
    var grossWinnings = match.CalculateWinnings(0); // Gross amount
    var commission = grossWinnings * commissionRate;
    var netWinnings = grossWinnings - commission;

    // Get winner user
    var winner = await _context.Users
        .Include(u => u.Wallet)
        .FirstOrDefaultAsync(u => u.Id == winnerBet.Bet.UserId);

    if (winner == null)
    {
        throw new InvalidOperationException("Winner user not found");
    }

    // Credit winnings to winner
    winner.Wallet!.RecordWinnings(new Money(
        match.MatchedStake + netWinnings,
        winner.Wallet.Balance.Currency));

    // Record transaction
    var transaction = new Transaction(
        winner,
        TransactionType.BetWin,
        new Money(netWinnings, winner.Wallet.Balance.Currency),
        winner.Wallet.Balance,
        $"Exchange bet win: Match {match.Id}",
        TransactionStatus.Completed);

    _context.Transactions.Add(transaction);

    // Mark match as settled
    match.Settle(backBetWins);

    // Update bet statuses
    winnerBet.Bet.SetStatus(BetStatus.Won);
    loserBet.Bet.SetStatus(BetStatus.Lost);

    await _context.SaveChangesAsync();

    _logger.LogInformation(
        "Settled match {MatchId}: Winner {WinnerId}, Payout {Payout}, Commission {Commission}",
        match.Id, winner.Id, netWinnings, commission);
}

public async Task SettleAllExchangeMatches(Guid eventId)
{
    var evt = await _context.Events
        .Include(e => e.Markets)
            .ThenInclude(m => m.Outcomes)
        .FirstOrDefaultAsync(e => e.Id == eventId);

    if (evt == null)
    {
        throw new ArgumentException("Event not found");
    }

    if (evt.Status != EventStatus.Completed)
    {
        throw new InvalidOperationException("Event not completed yet");
    }

    // Get all unsettled matches for this event
    var outcomeIds = evt.Markets.SelectMany(m => m.Outcomes).Select(o => o.Id).ToList();

    var matches = await _context.BetMatches
        .Include(m => m.BackBet)
            .ThenInclude(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
        .Include(m => m.LayBet)
            .ThenInclude(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
        .Where(m => !m.IsSettled)
        .Where(m => m.BackBet.Bet.Selections.Any(s => outcomeIds.Contains(s.OutcomeId)))
        .ToListAsync();

    var market = evt.Markets.FirstOrDefault();
    var commissionRate = market?.ExchangeCommissionRate ?? 0.02m;

    foreach (var match in matches)
    {
        await SettleExchangeMatch(match, evt, commissionRate);
    }

    _logger.LogInformation(
        "Settled {Count} exchange matches for event {EventId}",
        matches.Count, eventId);
}
```

### Step 4: Register Services

**File**: `SportsBetting.API/Program.cs` (MODIFY)

```csharp
// Add after existing service registrations
builder.Services.AddScoped<IBetMatchingService, BetMatchingService>();
builder.Services.AddScoped<IOddsValidationService, OddsValidationService>();
builder.Services.AddMemoryCache(); // For consensus odds caching
```

---

## Phase 4: API Layer Changes

**Duration**: 2 weeks
**Risk**: Low

### Create Exchange Controller

**File**: `SportsBetting.API/Controllers/ExchangeController.cs` (NEW)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SportsBetting.API.DTOs;
using SportsBetting.Data;
using SportsBetting.Domain.Configuration;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.ValueObjects;
using System.Security.Claims;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ExchangeController : ControllerBase
{
    private readonly SportsBettingDbContext _context;
    private readonly IBetMatchingService _matchingService;
    private readonly IOddsValidationService _oddsValidation;
    private readonly WalletService _walletService;
    private readonly FeatureFlags _featureFlags;
    private readonly ILogger<ExchangeController> _logger;

    public ExchangeController(
        SportsBettingDbContext context,
        IBetMatchingService matchingService,
        IOddsValidationService oddsValidation,
        WalletService walletService,
        IOptions<FeatureFlags> featureFlags,
        ILogger<ExchangeController> logger)
    {
        _context = context;
        _matchingService = matchingService;
        _oddsValidation = oddsValidation;
        _walletService = walletService;
        _featureFlags = featureFlags.Value;
        _logger = logger;
    }

    /// <summary>
    /// Place an exchange bet (back or lay)
    /// </summary>
    [HttpPost("bets")]
    [ProducesResponseType<ExchangeBetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ExchangeBetResponse>> PlaceExchangeBet(
        [FromBody] PlaceExchangeBetRequest request)
    {
        // Feature flag check
        if (!_featureFlags.EnableExchange)
        {
            return BadRequest(new { message = "Exchange betting not enabled" });
        }

        var userId = GetCurrentUserId();

        // Validate market is exchange-enabled
        var market = await _context.Markets
            .Include(m => m.Outcomes)
            .FirstOrDefaultAsync(m => m.Outcomes.Any(o => o.Id == request.OutcomeId));

        if (market == null)
            return NotFound(new { message = "Market not found" });

        if (market.Mode == MarketMode.Sportsbook)
            return BadRequest(new { message = "This market is sportsbook-only" });

        // Validate odds
        if (_featureFlags.ConsensusOddsValidation)
        {
            var validation = await _oddsValidation.ValidateOdds(
                request.OutcomeId,
                request.ProposedOdds);

            if (!validation.IsValid)
            {
                return BadRequest(new { message = validation.Reason });
            }
        }

        // Create bet and exchange bet
        var user = await _context.Users
            .Include(u => u.Wallet)
            .FirstOrDefaultAsync(u => u.Id == userId);

        var outcome = market.Outcomes.First(o => o.Id == request.OutcomeId);
        var evt = await _context.Events
            .Include(e => e.Markets)
            .FirstOrDefaultAsync(e => e.Markets.Contains(market));

        var bet = Bet.CreateExchangeSingle(
            user!,
            new Money(request.Stake, user.Wallet!.Balance.Currency),
            evt!,
            market,
            outcome,
            request.ProposedOdds,
            request.Side);

        var exchangeBet = new ExchangeBet(
            bet,
            request.Side,
            request.ProposedOdds,
            request.Stake);

        _context.Bets.Add(bet);
        _context.ExchangeBets.Add(exchangeBet);

        // Deduct stake from wallet
        var transaction = _walletService.PlaceBet(user, bet);
        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();

        // Try to match if auto-matching enabled
        MatchResult? matchResult = null;
        if (_featureFlags.AutoMatching)
        {
            matchResult = await _matchingService.MatchBet(exchangeBet);
        }

        return CreatedAtAction(
            nameof(GetExchangeBet),
            new { id = exchangeBet.Id },
            MapToExchangeBetResponse(exchangeBet, matchResult));
    }

    /// <summary>
    /// Get exchange bet by ID
    /// </summary>
    [HttpGet("bets/{id}")]
    [ProducesResponseType<ExchangeBetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExchangeBetResponse>> GetExchangeBet(Guid id)
    {
        var exchangeBet = await _context.ExchangeBets
            .Include(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
            .Include(eb => eb.MatchesAsBack)
            .Include(eb => eb.MatchesAsLay)
            .FirstOrDefaultAsync(eb => eb.Id == id);

        if (exchangeBet == null)
            return NotFound();

        return Ok(MapToExchangeBetResponse(exchangeBet, null));
    }

    /// <summary>
    /// Get orderbook for an outcome
    /// </summary>
    [HttpGet("outcomes/{outcomeId}/orderbook")]
    [AllowAnonymous]
    [ProducesResponseType<OrderbookResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderbookResponse>> GetOrderbook(Guid outcomeId)
    {
        var backBets = await _matchingService.GetUnmatchedBets(
            outcomeId, BetSide.Back);
        var layBets = await _matchingService.GetUnmatchedBets(
            outcomeId, BetSide.Lay);

        var consensus = await _oddsValidation.GetConsensusOdds(outcomeId);

        return Ok(new OrderbookResponse
        {
            OutcomeId = outcomeId,
            BackBets = backBets.Select(MapToOrderbookEntry).ToList(),
            LayBets = layBets.Select(MapToOrderbookEntry).ToList(),
            ConsensusOdds = consensus != null ? new ConsensusOddsInfo
            {
                AverageOdds = consensus.AverageOdds,
                Source = consensus.Source,
                FetchedAt = consensus.FetchedAt
            } : null
        });
    }

    /// <summary>
    /// Take (match) an existing exchange bet
    /// </summary>
    [HttpPost("bets/{id}/take")]
    [ProducesResponseType<MatchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MatchResponse>> TakeBet(
        Guid id,
        [FromBody] TakeBetRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _matchingService.TakeBet(id, userId, request.Stake);

            return Ok(new MatchResponse
            {
                Success = true,
                MatchedStake = result.MatchedAmount,
                Message = result.Message,
                Matches = result.Matches.Select(m => new MatchInfo
                {
                    MatchId = m.Id,
                    MatchedStake = m.MatchedStake,
                    MatchedOdds = m.MatchedOdds,
                    MatchedAt = m.MatchedAt
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancel an unmatched or partially matched bet
    /// </summary>
    [HttpDelete("bets/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelBet(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _matchingService.CancelBet(id, userId);

            return Ok(new { message = "Bet cancelled successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get my exchange bets
    /// </summary>
    [HttpGet("my-bets")]
    [ProducesResponseType<List<ExchangeBetResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExchangeBetResponse>>> GetMyBets(
        [FromQuery] BetState? state = null)
    {
        var userId = GetCurrentUserId();

        var query = _context.ExchangeBets
            .Include(eb => eb.Bet)
                .ThenInclude(b => b.Selections)
            .Include(eb => eb.MatchesAsBack)
            .Include(eb => eb.MatchesAsLay)
            .Where(eb => eb.Bet.UserId == userId);

        if (state.HasValue)
        {
            query = query.Where(eb => eb.State == state.Value);
        }

        var bets = await query
            .OrderByDescending(eb => eb.CreatedAt)
            .ToListAsync();

        return Ok(bets.Select(eb => MapToExchangeBetResponse(eb, null)).ToList());
    }

    // Helper methods
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user ID in token");
        }
        return userId;
    }

    private ExchangeBetResponse MapToExchangeBetResponse(
        ExchangeBet eb,
        MatchResult? matchResult)
    {
        return new ExchangeBetResponse
        {
            ExchangeBetId = eb.Id,
            BetId = eb.BetId,
            OutcomeId = eb.Bet.Selections.First().OutcomeId,
            OutcomeName = eb.Bet.Selections.First().OutcomeName,
            Side = eb.Side.ToString(),
            ProposedOdds = eb.ProposedOdds,
            TotalStake = eb.TotalStake,
            MatchedStake = eb.MatchedStake,
            UnmatchedStake = eb.UnmatchedStake,
            State = eb.State.ToString(),
            PlacedAt = eb.CreatedAt,
            MatchResult = matchResult != null ? new MatchResultInfo
            {
                FullyMatched = matchResult.FullyMatched,
                MatchedAmount = matchResult.MatchedAmount,
                UnmatchedAmount = matchResult.UnmatchedAmount,
                Message = matchResult.Message
            } : null
        };
    }

    private OrderbookEntry MapToOrderbookEntry(ExchangeBet eb)
    {
        return new OrderbookEntry
        {
            ExchangeBetId = eb.Id,
            Odds = eb.ProposedOdds,
            AvailableStake = eb.UnmatchedStake,
            PlacedAt = eb.CreatedAt
        };
    }
}
```

### Create DTOs

**File**: `SportsBetting.API/DTOs/ExchangeDTOs.cs` (NEW)

```csharp
namespace SportsBetting.API.DTOs;

public record PlaceExchangeBetRequest(
    Guid OutcomeId,
    BetSide Side,
    decimal Stake,
    decimal ProposedOdds);

public record TakeBetRequest(decimal Stake);

public record ExchangeBetResponse
{
    public Guid ExchangeBetId { get; init; }
    public Guid BetId { get; init; }
    public Guid OutcomeId { get; init; }
    public string OutcomeName { get; init; } = string.Empty;
    public string Side { get; init; } = string.Empty;
    public decimal ProposedOdds { get; init; }
    public decimal TotalStake { get; init; }
    public decimal MatchedStake { get; init; }
    public decimal UnmatchedStake { get; init; }
    public string State { get; init; } = string.Empty;
    public DateTime PlacedAt { get; init; }
    public MatchResultInfo? MatchResult { get; init; }
}

public record MatchResultInfo
{
    public bool FullyMatched { get; init; }
    public decimal MatchedAmount { get; init; }
    public decimal UnmatchedAmount { get; init; }
    public string Message { get; init; } = string.Empty;
}

public record OrderbookResponse
{
    public Guid OutcomeId { get; init; }
    public List<OrderbookEntry> BackBets { get; init; } = new();
    public List<OrderbookEntry> LayBets { get; init; } = new();
    public ConsensusOddsInfo? ConsensusOdds { get; init; }
}

public record OrderbookEntry
{
    public Guid ExchangeBetId { get; init; }
    public decimal Odds { get; init; }
    public decimal AvailableStake { get; init; }
    public DateTime PlacedAt { get; init; }
}

public record ConsensusOddsInfo
{
    public decimal AverageOdds { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTime FetchedAt { get; init; }
}

public record MatchResponse
{
    public bool Success { get; init; }
    public decimal MatchedStake { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<MatchInfo> Matches { get; init; } = new();
}

public record MatchInfo
{
    public Guid MatchId { get; init; }
    public decimal MatchedStake { get; init; }
    public decimal MatchedOdds { get; init; }
    public DateTime MatchedAt { get; init; }
}
```

---

## Phase 5: Settlement Logic Dual-Mode

**Duration**: 1 week
**Risk**: High

### Update BetsController Settlement

**File**: `SportsBetting.API/Controllers/BetsController.cs` (MODIFY)

Update the `SettleBet` method:

```csharp
[HttpPost("{id}/settle")]
[Authorize(Policy = "AdminOnly")]
public async Task<ActionResult<BetResponse>> SettleBet(Guid id)
{
    var bet = await _context.Bets
        .Include(b => b.Selections)
        .FirstOrDefaultAsync(b => b.Id == id);

    if (bet == null)
    {
        return NotFound(new { message = $"Bet {id} not found" });
    }

    // NEW: Check bet mode
    if (bet.BetMode == BetMode.Exchange)
    {
        return BadRequest(new
        {
            message = "Exchange bets are settled via match settlement. Use POST /api/events/{eventId}/settle-exchange instead."
        });
    }

    // Existing sportsbook settlement logic
    var eventIds = bet.Selections.Select(s => s.EventId).Distinct();
    var events = await _context.Events
        .Include(e => e.Markets)
            .ThenInclude(m => m.Outcomes)
        .Where(e => eventIds.Contains(e.Id))
        .ToListAsync();

    // ... rest of existing code ...
}
```

### Add Event-Level Exchange Settlement

**File**: `SportsBetting.API/Controllers/EventsController.cs` (MODIFY)

Add new endpoint:

```csharp
/// <summary>
/// Settle all exchange matches for an event (Admin only)
/// </summary>
[HttpPost("{id}/settle-exchange")]
[Authorize(Policy = "AdminOnly")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> SettleExchangeMatches(Guid id)
{
    try
    {
        await _settlementService.SettleAllExchangeMatches(id);

        return Ok(new
        {
            message = "Exchange matches settled successfully"
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

---

## Phase 6: Testing & Validation

**Duration**: 1 week
**Risk**: Medium

### Unit Tests

**File**: `SportsBetting.Tests/Services/BetMatchingServiceTests.cs` (NEW)

```csharp
using Xunit;
using Moq;
using SportsBetting.Domain.Services;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Enums;

namespace SportsBetting.Tests.Services;

public class BetMatchingServiceTests
{
    [Fact]
    public async Task MatchBet_BackWithLay_FullyMatches()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new BetMatchingService(context, Mock.Of<ILogger<BetMatchingService>>());

        var backBet = CreateExchangeBet(BetSide.Back, 100m, 2.0m);
        var layBet = CreateExchangeBet(BetSide.Lay, 100m, 2.0m);

        context.ExchangeBets.Add(layBet);
        await context.SaveChangesAsync();

        // Act
        var result = await service.MatchBet(backBet);

        // Assert
        Assert.True(result.FullyMatched);
        Assert.Equal(100m, result.MatchedAmount);
        Assert.Equal(0m, result.UnmatchedAmount);
    }

    [Fact]
    public async Task MatchBet_PartialMatch_CorrectlyDivides()
    {
        // Test partial matching logic
        // TODO: Implement
    }

    // Add more tests...
}
```

### Integration Tests

**File**: `SportsBetting.Tests/Integration/ExchangeEndToEndTests.cs` (NEW)

```csharp
[Fact]
public async Task PlaceExchangeBet_ThenTake_SuccessfullyMatches()
{
    // End-to-end test: Alice places back bet, Bob takes it
    // TODO: Implement
}
```

### Manual Testing Checklist

Create file: `TESTING_CHECKLIST.md`

```markdown
# Exchange Feature Testing Checklist

## Phase 1: Database
- [ ] Migrations apply cleanly
- [ ] Backward compatibility: existing bets still work
- [ ] Indexes created
- [ ] Constraints enforced

## Phase 2: Core Services
- [ ] Matching service matches compatible bets
- [ ] Partial matching works correctly
- [ ] Odds validation rejects bad odds
- [ ] Consensus odds caching works

## Phase 3: API
- [ ] Place exchange bet (back)
- [ ] Place exchange bet (lay)
- [ ] View orderbook
- [ ] Take existing bet
- [ ] Cancel unmatched bet
- [ ] Cannot cancel matched bet

## Phase 4: Settlement
- [ ] Exchange matches settle correctly
- [ ] Commission calculated properly
- [ ] Winner gets payout
- [ ] Loser loses stake

## Phase 5: Security
- [ ] Feature flag disables exchange when off
- [ ] Cannot match own bet
- [ ] Cannot cancel other user's bet
- [ ] Admins can settle matches

## Phase 6: Performance
- [ ] Orderbook loads quickly (< 200ms)
- [ ] Matching completes in < 500ms
- [ ] Consensus odds cached
```

---

## Phase 7: Deployment Strategy

**Duration**: 1 week
**Risk**: High

### Deployment Steps

```bash
# 1. Deploy to staging
git push origin feature/hybrid-betting-system

# 2. Run migrations on staging
dotnet ef database update --connection $STAGING_DB

# 3. Smoke test on staging
./scripts/smoke_test_exchange.sh staging

# 4. Feature flag rollout (gradual)
# appsettings.Production.json
{
  "FeatureFlags": {
    "EnableExchange": true,
    "EnableHybridMode": false,  // Start with exchange-only mode
    "ExchangeMarketsWhitelist": ["<specific-market-guid>"]  // Single market
  }
}

# 5. Monitor for 24 hours
# Check metrics: error rates, match rates, settlement accuracy

# 6. Expand to more markets
# Add more GUIDs to whitelist

# 7. Enable hybrid mode
{
  "EnableHybridMode": true
}

# 8. Full rollout
{
  "ExchangeMarketsWhitelist": []  // Empty = all markets
}
```

### Monitoring Alerts

```yaml
# prometheus alerts
- alert: ExchangeMatchFailureRate
  expr: rate(exchange_match_failures_total[5m]) > 0.1
  annotations:
    summary: "High exchange match failure rate"

- alert: ConsensusOddsStale
  expr: time() - consensus_odds_last_fetch_timestamp > 600
  annotations:
    summary: "Consensus odds not updated in 10 minutes"

- alert: UnmatchedBetsAccumulating
  expr: exchange_unmatched_bets_total > 1000
  annotations:
    summary: "Large number of unmatched bets - liquidity issue"
```

---

## Rollback Plan

### If Things Go Wrong

**Scenario 1: Exchange causing crashes**

```bash
# Immediate: Disable via feature flag
curl -X POST https://api.sportsbetting.com/admin/feature-flags \
  -d '{"EnableExchange": false}'

# No code rollback needed
```

**Scenario 2: Data corruption**

```bash
# Restore from backup
pg_restore -U calebwilliams -d sportsbetting backup_pre_hybrid.sql

# Rollback migration
dotnet ef database update <previous-migration-name>
```

**Scenario 3: Settlement errors**

```bash
# Disable settlement
{
  "FeatureFlags": {
    "EnableExchange": true,
    "EnableSettlement": false  // Add this flag
  }
}

# Fix issues, manually settle affected matches
# Re-enable when fixed
```

---

## Success Criteria

Exchange feature is considered successfully deployed when:

- [ ] 100+ successful matches completed
- [ ] 0 settlement errors in 7 days
- [ ] < 0.1% match failure rate
- [ ] < 200ms avg orderbook load time
- [ ] Consensus odds coverage > 90%
- [ ] User complaints < 5 per week
- [ ] Commission revenue > $X/month

---

## Appendix: Configuration Reference

**Complete appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sportsbetting;Username=calebwilliams"
  },
  "FeatureFlags": {
    "EnableExchange": true,
    "EnableHybridMode": true,
    "ExchangeMarketsWhitelist": [],
    "ConsensusOddsValidation": true,
    "AutoMatching": true,
    "AllowPartialMatching": true,
    "DefaultCommissionRate": 0.02,
    "OddsTolerancePercent": 10.0
  },
  "ExternalAPIs": {
    "TheOddsAPI": {
      "ApiKey": "your-api-key-here",
      "BaseUrl": "https://api.the-odds-api.com/v4",
      "CacheTTLMinutes": 5
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SportsBetting.Domain.Services.BetMatchingService": "Debug",
      "SportsBetting.Domain.Services.SettlementService": "Debug"
    }
  }
}
```

---

**END OF MIGRATION PLAN**

This document should be updated as implementation progresses.
Next review: Before starting Phase 1.
