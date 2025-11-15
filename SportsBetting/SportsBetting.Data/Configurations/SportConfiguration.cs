using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    public void Configure(EntityTypeBuilder<Sport> builder)
    {
        builder.ToTable("Sports");

        // Primary key
        builder.HasKey(s => s.Id);

        // Properties
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(20);

        // Indexes
        builder.HasIndex(s => s.Code)
            .IsUnique()
            .HasDatabaseName("IX_Sports_Code");

        builder.HasIndex(s => s.Name)
            .HasDatabaseName("IX_Sports_Name");

        // Relationships
        builder.HasMany<League>()
            .WithOne()
            .HasForeignKey(l => l.SportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
