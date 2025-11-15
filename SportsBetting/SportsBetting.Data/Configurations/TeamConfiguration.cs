using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.City)
            .HasMaxLength(100);

        builder.Property(t => t.LeagueId)
            .IsRequired();

        // Indexes
        builder.HasIndex(t => t.Code)
            .IsUnique()
            .HasDatabaseName("IX_Teams_Code");

        builder.HasIndex(t => t.LeagueId)
            .HasDatabaseName("IX_Teams_LeagueId");

        builder.HasIndex(t => t.Name)
            .HasDatabaseName("IX_Teams_Name");
    }
}
