using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Configurations;

public class BetConfiguration : IEntityTypeConfiguration<Bet>
{
    public void Configure(EntityTypeBuilder<Bet> builder)
    {
        builder.ToTable("Bets");

        // Primary key
        builder.HasKey(b => b.Id);

        // Properties
        builder.Property(b => b.UserId)
            .IsRequired();

        builder.Property(b => b.TicketNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.Type)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(b => b.PlacedAt)
            .IsRequired();

        builder.Property(b => b.SettledAt);

        builder.Property(b => b.LineLockId);

        // Money value objects as owned types
        builder.ComplexProperty(b => b.Stake, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("Stake")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("StakeCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.ComplexProperty(b => b.PotentialPayout, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("PotentialPayout")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("PotentialPayoutCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        // ActualPayout is nullable Money - map to separate nullable columns
        builder.Property(b => b.ActualPayout)
            .HasConversion(
                v => v.HasValue ? v.Value.Amount : (decimal?)null,
                v => v.HasValue ? new Money(v.Value, "USD") : null)
            .HasColumnName("ActualPayout")
            .HasPrecision(18, 2);

        // Currency for ActualPayout stored separately (workaround for nullable struct)
        builder.Property<string?>("ActualPayoutCurrency")
            .HasMaxLength(3)
            .IsFixedLength();

        // Odds value object as owned type
        builder.ComplexProperty(b => b.CombinedOdds, oddsBuilder =>
        {
            oddsBuilder.Property(odds => odds.DecimalValue)
                .HasColumnName("CombinedOddsDecimal")
                .HasPrecision(10, 4)
                .IsRequired();
        });

        // Relationships
        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<BetSelection>()
            .WithOne()
            .HasForeignKey(bs => bs.BetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(b => b.UserId)
            .HasDatabaseName("IX_Bets_UserId");

        builder.HasIndex(b => b.TicketNumber)
            .IsUnique()
            .HasDatabaseName("IX_Bets_TicketNumber");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_Bets_Status");

        builder.HasIndex(b => b.PlacedAt)
            .HasDatabaseName("IX_Bets_PlacedAt");

        builder.HasIndex(b => new { b.UserId, b.PlacedAt })
            .HasDatabaseName("IX_Bets_UserId_PlacedAt");

        builder.HasIndex(b => b.LineLockId)
            .HasDatabaseName("IX_Bets_LineLockId");

        // Check constraints (PostgreSQL syntax with double quotes)
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Bets_Stake_Positive", "\"Stake\" > 0");
            t.HasCheckConstraint("CK_Bets_PotentialPayout_NonNegative", "\"PotentialPayout\" >= 0");
            t.HasCheckConstraint("CK_Bets_CombinedOdds_MinimumOne", "\"CombinedOddsDecimal\" >= 1.0");
            t.HasCheckConstraint("CK_Bets_ActualPayout_NonNegative", "\"ActualPayout\" IS NULL OR \"ActualPayout\" >= 0");
        });
    }
}
