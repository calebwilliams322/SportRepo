using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.ToTable("Markets");

        // Primary key
        builder.HasKey(m => m.Id);

        // Properties
        builder.Property(m => m.EventId)
            .IsRequired();

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.Description)
            .HasMaxLength(500);

        builder.Property(m => m.Type)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(m => m.IsOpen)
            .IsRequired();

        builder.Property(m => m.IsSettled)
            .IsRequired();

        // Indexes
        builder.HasIndex(m => m.EventId)
            .HasDatabaseName("IX_Markets_EventId");

        builder.HasIndex(m => m.Type)
            .HasDatabaseName("IX_Markets_Type");

        builder.HasIndex(m => m.IsOpen)
            .HasDatabaseName("IX_Markets_IsOpen");

        builder.HasIndex(m => new { m.EventId, m.Type })
            .HasDatabaseName("IX_Markets_EventId_Type");

        // Relationships
        builder.HasMany<Outcome>()
            .WithOne()
            .HasForeignKey(o => o.MarketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
