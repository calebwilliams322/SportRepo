using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SportsBetting.Domain.Entities;

namespace SportsBetting.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        // Primary key
        builder.HasKey(w => w.Id);

        // Properties
        builder.Property(w => w.UserId)
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        builder.Property(w => w.LastUpdatedAt)
            .IsRequired();

        // Concurrency token for optimistic locking - maps to PostgreSQL's xmin system column
        builder.Property(w => w.RowVersion)
            .HasColumnType("xid")
            .HasColumnName("xmin")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Money value objects as complex types
        builder.ComplexProperty(w => w.Balance, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("Balance")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.ComplexProperty(w => w.TotalDeposited, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("TotalDeposited")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("TotalDepositedCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.ComplexProperty(w => w.TotalWithdrawn, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("TotalWithdrawn")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("TotalWithdrawnCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.ComplexProperty(w => w.TotalBet, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("TotalBet")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("TotalBetCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.ComplexProperty(w => w.TotalWon, moneyBuilder =>
        {
            moneyBuilder.Property(m => m.Amount)
                .HasColumnName("TotalWon")
                .HasPrecision(18, 2)
                .IsRequired();

            moneyBuilder.Property(m => m.Currency)
                .HasColumnName("TotalWonCurrency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        // Indexes
        builder.HasIndex(w => w.UserId)
            .IsUnique()
            .HasDatabaseName("IX_Wallets_UserId");

        builder.HasIndex(w => w.LastUpdatedAt)
            .HasDatabaseName("IX_Wallets_LastUpdatedAt");

        // Check constraints (PostgreSQL syntax with double quotes)
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Wallets_Balance_NonNegative", "\"Balance\" >= 0");
            t.HasCheckConstraint("CK_Wallets_TotalDeposited_NonNegative", "\"TotalDeposited\" >= 0");
            t.HasCheckConstraint("CK_Wallets_TotalWithdrawn_NonNegative", "\"TotalWithdrawn\" >= 0");
            t.HasCheckConstraint("CK_Wallets_TotalBet_NonNegative", "\"TotalBet\" >= 0");
            t.HasCheckConstraint("CK_Wallets_TotalWon_NonNegative", "\"TotalWon\" >= 0");
        });
    }
}
