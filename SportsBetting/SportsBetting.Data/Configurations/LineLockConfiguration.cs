using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class LineLockConfiguration : IEntityTypeConfiguration<LineLock>
{
    public void Configure(EntityTypeBuilder<LineLock> builder)
    {
        builder.ToTable("LineLocks");

        // Primary key
        builder.HasKey(ll => ll.Id);

        // Properties
        builder.Property(ll => ll.UserId)
            .IsRequired();

        builder.Property(ll => ll.LockNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ll => ll.EventId)
            .IsRequired();

        builder.Property(ll => ll.EventName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ll => ll.MarketId)
            .IsRequired();

        builder.Property(ll => ll.MarketType)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(ll => ll.MarketName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ll => ll.OutcomeId)
            .IsRequired();

        builder.Property(ll => ll.OutcomeName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ll => ll.Line)
            .HasPrecision(10, 2);

        builder.Property(ll => ll.ExpirationTime)
            .IsRequired();

        builder.Property(ll => ll.CreatedAt)
            .IsRequired();

        builder.Property(ll => ll.Status)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(ll => ll.AssociatedBetId);

        builder.Property(ll => ll.SettledAt);

        // Odds value object as owned type
        builder.ComplexProperty(ll => ll.LockedOdds, oddsBuilder =>
        {
            oddsBuilder.Property(odds => odds.DecimalValue)
                .HasColumnName("LockedOddsDecimal")
                .HasPrecision(10, 4)
                .IsRequired();
        });

        // Money value objects as owned types
        builder.ComplexProperty(ll => ll.LockFee, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("LockFee")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("LockFeeCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.ComplexProperty(ll => ll.MaxStake, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("MaxStake")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("MaxStakeCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        // Relationships
        builder.HasOne(ll => ll.User)
            .WithMany()
            .HasForeignKey(ll => ll.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ll => ll.UserId)
            .HasDatabaseName("IX_LineLocks_UserId");

        builder.HasIndex(ll => ll.LockNumber)
            .IsUnique()
            .HasDatabaseName("IX_LineLocks_LockNumber");

        builder.HasIndex(ll => ll.Status)
            .HasDatabaseName("IX_LineLocks_Status");

        builder.HasIndex(ll => ll.ExpirationTime)
            .HasDatabaseName("IX_LineLocks_ExpirationTime");

        builder.HasIndex(ll => ll.EventId)
            .HasDatabaseName("IX_LineLocks_EventId");

        builder.HasIndex(ll => ll.AssociatedBetId)
            .HasDatabaseName("IX_LineLocks_AssociatedBetId");

        builder.HasIndex(ll => new { ll.UserId, ll.Status })
            .HasDatabaseName("IX_LineLocks_UserId_Status");
    }
}
