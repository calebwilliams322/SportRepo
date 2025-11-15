using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.ValueObjects;

namespace SportsBetting.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    private static Score ParseScore(string value)
    {
        var parts = value.Split(':');
        return new Score(int.Parse(parts[0]), int.Parse(parts[1]));
    }

    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.LeagueId)
            .IsRequired();

        builder.Property(e => e.ScheduledStartTime)
            .IsRequired();

        builder.Property(e => e.Venue)
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        // Score value object (nullable struct - configured inline)
        builder.Property(e => e.FinalScore)
            .HasConversion(
                v => v.HasValue ? $"{v.Value.HomeScore}:{v.Value.AwayScore}" : null,
                v => v != null ? ParseScore(v) : null)
            .HasColumnName("FinalScore")
            .HasMaxLength(20);

        // Team relationships with shadow foreign keys
        builder.HasOne(e => e.HomeTeam)
            .WithMany()
            .HasForeignKey("HomeTeamId") // Shadow property
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AwayTeam)
            .WithMany()
            .HasForeignKey("AwayTeamId") // Shadow property
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.LeagueId)
            .HasDatabaseName("IX_Events_LeagueId");

        builder.HasIndex(e => e.ScheduledStartTime)
            .HasDatabaseName("IX_Events_ScheduledStartTime");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Events_Status");

        builder.HasIndex("HomeTeamId")
            .HasDatabaseName("IX_Events_HomeTeamId");

        builder.HasIndex("AwayTeamId")
            .HasDatabaseName("IX_Events_AwayTeamId");

        // Composite index for common queries
        builder.HasIndex(e => new { e.LeagueId, e.ScheduledStartTime })
            .HasDatabaseName("IX_Events_LeagueId_ScheduledStartTime");

        // Relationships
        builder.HasMany<Market>()
            .WithOne()
            .HasForeignKey(m => m.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
