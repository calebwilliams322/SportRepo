using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class OutcomeConfiguration : IEntityTypeConfiguration<Outcome>
{
    public void Configure(EntityTypeBuilder<Outcome> builder)
    {
        builder.ToTable("Outcomes");

        // Primary key
        builder.HasKey(o => o.Id);

        // Properties
        builder.Property(o => o.MarketId)
            .IsRequired();

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.Line)
            .HasPrecision(10, 2);

        builder.Property(o => o.IsWinner);

        builder.Property(o => o.IsVoid)
            .IsRequired();

        // Odds API integration properties
        builder.Property(o => o.ExternalId)
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(o => o.LastOddsUpdate)
            .IsRequired(false);

        // Odds value object as owned type
        builder.ComplexProperty(o => o.CurrentOdds, oddsBuilder =>
        {
            oddsBuilder.Property(odds => odds.DecimalValue)
                .HasColumnName("CurrentOddsDecimal")
                .HasPrecision(10, 4)
                .IsRequired();
        });

        // Indexes
        builder.HasIndex(o => o.MarketId)
            .HasDatabaseName("IX_Outcomes_MarketId");

        builder.HasIndex(o => o.IsWinner)
            .HasDatabaseName("IX_Outcomes_IsWinner");

        builder.HasIndex(o => o.IsVoid)
            .HasDatabaseName("IX_Outcomes_IsVoid");

        // Odds API indexes
        builder.HasIndex(o => o.ExternalId)
            .HasDatabaseName("IX_Outcomes_ExternalId");

        builder.HasIndex(o => o.LastOddsUpdate)
            .HasDatabaseName("IX_Outcomes_LastOddsUpdate");
    }
}
