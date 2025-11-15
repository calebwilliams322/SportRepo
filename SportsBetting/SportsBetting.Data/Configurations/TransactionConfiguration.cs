using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Type)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(t => t.ReferenceId);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CompletedAt);

        // Money value objects as owned types
        builder.ComplexProperty(t => t.Amount, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("Amount")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.ComplexProperty(t => t.BalanceBefore, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("BalanceBefore")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("BalanceBeforeCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.ComplexProperty(t => t.BalanceAfter, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("BalanceAfter")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("BalanceAfterCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        // Relationships
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_Transactions_UserId");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("IX_Transactions_CreatedAt");

        builder.HasIndex(t => new { t.UserId, t.CreatedAt })
            .HasDatabaseName("IX_Transactions_UserId_CreatedAt");

        builder.HasIndex(t => t.ReferenceId)
            .HasDatabaseName("IX_Transactions_ReferenceId");

        builder.HasIndex(t => t.Type)
            .HasDatabaseName("IX_Transactions_Type");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_Transactions_Status");

        // Check constraints (PostgreSQL syntax with double quotes)
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Transactions_Amount_Positive", "\"Amount\" > 0");
            t.HasCheckConstraint("CK_Transactions_BalanceBefore_NonNegative", "\"BalanceBefore\" >= 0");
            t.HasCheckConstraint("CK_Transactions_BalanceAfter_NonNegative", "\"BalanceAfter\" >= 0");
        });
    }
}
