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

    // Sports Entities
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<League> Leagues => Set<League>();
    public DbSet<Team> Teams => Set<Team>();

    // Events and Markets
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Market> Markets => Set<Market>();
    public DbSet<Outcome> Outcomes => Set<Outcome>();

    // Betting
    public DbSet<Bet> Bets => Set<Bet>();
    public DbSet<BetSelection> BetSelections => Set<BetSelection>();
    public DbSet<LineLock> LineLocks => Set<LineLock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new WalletConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new SportConfiguration());
        modelBuilder.ApplyConfiguration(new LeagueConfiguration());
        modelBuilder.ApplyConfiguration(new TeamConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new MarketConfiguration());
        modelBuilder.ApplyConfiguration(new OutcomeConfiguration());
        modelBuilder.ApplyConfiguration(new BetConfiguration());
        modelBuilder.ApplyConfiguration(new BetSelectionConfiguration());
        modelBuilder.ApplyConfiguration(new LineLockConfiguration());
    }
}
