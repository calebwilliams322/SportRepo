using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class ExternalEventMappingConfiguration : IEntityTypeConfiguration<ExternalEventMapping>
{
    public void Configure(EntityTypeBuilder<ExternalEventMapping> builder)
    {
        builder.ToTable("ExternalEventMappings");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.EventId)
            .IsRequired();

        builder.Property(m => m.ExternalId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Provider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        builder.Property(m => m.LastVerifiedAt)
            .IsRequired();

        // Relationship with Event
        builder.HasOne(m => m.Event)
            .WithMany()
            .HasForeignKey(m => m.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for fast lookups
        builder.HasIndex(m => new { m.ExternalId, m.Provider })
            .IsUnique()
            .HasDatabaseName("IX_ExternalEventMappings_ExternalId_Provider");

        builder.HasIndex(m => m.EventId)
            .HasDatabaseName("IX_ExternalEventMappings_EventId");

        builder.HasIndex(m => m.Provider)
            .HasDatabaseName("IX_ExternalEventMappings_Provider");
    }
}
