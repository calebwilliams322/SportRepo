using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.ToTable("Leagues");

        // Primary key
        builder.HasKey(l => l.Id);

        // Properties
        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(l => l.SportId)
            .IsRequired();

        // Indexes
        builder.HasIndex(l => l.Code)
            .IsUnique()
            .HasDatabaseName("IX_Leagues_Code");

        builder.HasIndex(l => l.SportId)
            .HasDatabaseName("IX_Leagues_SportId");

        // Relationships
        builder.HasMany<Team>()
            .WithOne()
            .HasForeignKey(t => t.LeagueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
