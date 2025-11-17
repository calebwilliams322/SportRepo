using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class OddsHistoryConfiguration : IEntityTypeConfiguration<OddsHistory>
{
    public void Configure(EntityTypeBuilder<OddsHistory> builder)
    {
        builder.ToTable("OddsHistory");

        builder.HasKey(oh => oh.Id);

        builder.Property(oh => oh.OutcomeId)
            .IsRequired();

        builder.Property(oh => oh.Source)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(oh => oh.Timestamp)
            .IsRequired();

        builder.Property(oh => oh.RawBookmakerData)
            .HasColumnType("jsonb"); // PostgreSQL JSON type

        // Configure Odds as complex property
        builder.ComplexProperty(oh => oh.Odds, oddsBuilder =>
        {
            oddsBuilder.Property(odds => odds.DecimalValue)
                .HasColumnName("Odds")
                .HasPrecision(10, 4)
                .IsRequired();
        });

        // Foreign key to Outcome
        builder.HasOne(oh => oh.Outcome)
            .WithMany()
            .HasForeignKey(oh => oh.OutcomeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for common queries
        builder.HasIndex(oh => oh.OutcomeId)
            .HasDatabaseName("IX_OddsHistory_OutcomeId");

        builder.HasIndex(oh => oh.Timestamp)
            .HasDatabaseName("IX_OddsHistory_Timestamp");

        builder.HasIndex(oh => new { oh.OutcomeId, oh.Timestamp })
            .HasDatabaseName("IX_OddsHistory_OutcomeId_Timestamp");
    }
}
