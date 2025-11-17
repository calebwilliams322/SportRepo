using Microsoft.EntityFrameworkCore;
using SportsBetting.Domain.Entities;
using SportsBetting.Data.Configurations;

namespace SportsBetting.Data;

/// <summary>
/// Entity Framework Core DbContext for the Sports Betting system
/// </summary>
public class SportsBettingDbContext : DbContext
{
    public SportsBettingDbContext(DbContextOptions<SportsBettingDbContext> options)
        : base(options)
    {
    }

    // Users and Wallets
    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserStatistics> UserStatistics => Set<UserStatistics>();

    // Sports Entities
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Team> Teams => Set<Team>();

    // Events and Markets
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<Outcome> Outcomes => Set<Outcome>();
    public DbSet<OddsHistory> OddsHistory => Set<OddsHistory>();

    // Betting
    public DbSet<Bet> Bets => Set<Bet>();
    public DbSet<BetSelection> BetSelections => Set<BetSelection>();
    public DbSet<LineLock> LineLocks => Set<LineLock>();

    // Exchange Betting (P2P)
    public DbSet<ExchangeBet> ExchangeBets => Set<ExchangeBet>();
    public DbSet<BetMatch> BetMatches => Set<BetMatch>();
    public DbSet<ConsensusOdds> ConsensusOdds => Set<ConsensusOdds>();

    // Revenue Tracking
    public DbSet<HouseRevenue> HouseRevenue => Set<HouseRevenue>();

    // External Event Mappings (for ID-based matching)
    public DbSet<ExternalEventMapping> ExternalEventMappings => Set<ExternalEventMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new UserStatisticsConfiguration());
        modelBuilder.ApplyConfiguration(new WalletConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new SportConfiguration());
        modelBuilder.ApplyConfiguration(new LeagueConfiguration());
        modelBuilder.ApplyConfiguration(new TeamConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new MarketConfiguration());
        modelBuilder.ApplyConfiguration(new OutcomeConfiguration());
        modelBuilder.ApplyConfiguration(new OddsHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new BetConfiguration());
        modelBuilder.ApplyConfiguration(new BetSelectionConfiguration());
        modelBuilder.ApplyConfiguration(new LineLockConfiguration());
        modelBuilder.ApplyConfiguration(new ExternalEventMappingConfiguration());

        // Configure Exchange entities
        ConfigureExchangeEntities(modelBuilder);
    }

    private void ConfigureExchangeEntities(ModelBuilder modelBuilder)
    {
        // ExchangeBet configuration
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

            entity.HasIndex(e => new { e.State })
                  .HasFilter("\"State\" IN ('Unmatched', 'PartiallyMatched')");

            entity.HasIndex(e => e.BetId)
                  .IsUnique();
        });

        // BetMatch configuration
        modelBuilder.Entity<BetMatch>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.BackBet)
                  .WithMany(eb => eb.MatchesAsBack)
                  .HasForeignKey(e => e.BackBetId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.LayBet)
                  .WithMany(eb => eb.MatchesAsLay)
                  .HasForeignKey(e => e.LayBetId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.IsSettled)
                  .HasFilter("\"IsSettled\" = false");
        });

        // ConsensusOdds configuration
        modelBuilder.Entity<ConsensusOdds>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Outcome)
                  .WithMany()
                  .HasForeignKey(e => e.OutcomeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Source)
                  .HasMaxLength(50);

            entity.HasIndex(e => new { e.OutcomeId, e.ExpiresAt });
        });

        // HouseRevenue configuration
        modelBuilder.Entity<HouseRevenue>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PeriodType)
                  .HasMaxLength(20)
                  .IsRequired();

            // Index for efficient period lookups
            entity.HasIndex(e => new { e.PeriodStart, e.PeriodEnd, e.PeriodType })
                  .IsUnique();

            // Index for time-based queries
            entity.HasIndex(e => e.PeriodStart);
        });
    }
}
