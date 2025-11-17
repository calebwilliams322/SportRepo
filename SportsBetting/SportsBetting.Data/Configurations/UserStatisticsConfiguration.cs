using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class UserStatisticsConfiguration : IEntityTypeConfiguration<UserStatistics>
{
    public void Configure(EntityTypeBuilder<UserStatistics> builder)
    {
        builder.ToTable("UserStatistics");

        // Primary key (don't auto-generate - Guid is set in constructor)
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        // Properties
        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.TotalVolumeAllTime)
            .HasPrecision(18, 2);

        builder.Property(s => s.Volume30Day)
            .HasPrecision(18, 2);

        builder.Property(s => s.Volume7Day)
            .HasPrecision(18, 2);

        builder.Property(s => s.TotalCommissionPaidAllTime)
            .HasPrecision(18, 2);

        builder.Property(s => s.Commission30Day)
            .HasPrecision(18, 2);

        builder.Property(s => s.MakerVolumeAllTime)
            .HasPrecision(18, 2);

        builder.Property(s => s.TakerVolumeAllTime)
            .HasPrecision(18, 2);

        builder.Property(s => s.NetProfitAllTime)
            .HasPrecision(18, 2);

        builder.Property(s => s.LargestWin)
            .HasPrecision(18, 2);

        builder.Property(s => s.LargestLoss)
            .HasPrecision(18, 2);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.LastUpdated)
            .IsRequired();

        // Indexes
        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserStatistics_UserId");

        builder.HasIndex(s => s.Volume30Day)
            .HasDatabaseName("IX_UserStatistics_Volume30Day");

        builder.HasIndex(s => s.LastUpdated)
            .HasDatabaseName("IX_UserStatistics_LastUpdated");

        // Relationships
        builder.HasOne(s => s.User)
            .WithOne(u => u.Statistics)
            .HasForeignKey<UserStatistics>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
