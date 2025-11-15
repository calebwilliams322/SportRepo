using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class BetSelectionConfiguration : IEntityTypeConfiguration<BetSelection>
{
    public void Configure(EntityTypeBuilder<BetSelection> builder)
    {
        builder.ToTable("BetSelections");

        // Primary key
        builder.HasKey(bs => bs.Id);

        // Properties
        builder.Property(bs => bs.BetId)
            .IsRequired();

        builder.Property(bs => bs.EventId)
            .IsRequired();

        builder.Property(bs => bs.EventName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bs => bs.MarketId)
            .IsRequired();

        builder.Property(bs => bs.MarketType)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(bs => bs.MarketName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bs => bs.OutcomeId)
            .IsRequired();

        builder.Property(bs => bs.OutcomeName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(bs => bs.Line)
            .HasPrecision(10, 2);

        builder.Property(bs => bs.Result)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        // Odds value object as owned type
        builder.ComplexProperty(bs => bs.LockedOdds, oddsBuilder =>
        {
            oddsBuilder.Property(odds => odds.DecimalValue)
                .HasColumnName("LockedOddsDecimal")
                .HasPrecision(10, 4)
                .IsRequired();
        });

        // Indexes
        builder.HasIndex(bs => bs.BetId)
            .HasDatabaseName("IX_BetSelections_BetId");

        builder.HasIndex(bs => bs.EventId)
            .HasDatabaseName("IX_BetSelections_EventId");

        builder.HasIndex(bs => bs.MarketId)
            .HasDatabaseName("IX_BetSelections_MarketId");

        builder.HasIndex(bs => bs.OutcomeId)
            .HasDatabaseName("IX_BetSelections_OutcomeId");

        builder.HasIndex(bs => bs.Result)
            .HasDatabaseName("IX_BetSelections_Result");
    }
}
