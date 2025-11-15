using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_TotalBet_NonNegative",
                table: "Wallets",
                sql: "\"TotalBet\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_TotalDeposited_NonNegative",
                table: "Wallets",
                sql: "\"TotalDeposited\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_TotalWithdrawn_NonNegative",
                table: "Wallets",
                sql: "\"TotalWithdrawn\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wallets_TotalWon_NonNegative",
                table: "Wallets",
                sql: "\"TotalWon\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_Amount_Positive",
                table: "Transactions",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_BalanceAfter_NonNegative",
                table: "Transactions",
                sql: "\"BalanceAfter\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transactions_BalanceBefore_NonNegative",
                table: "Transactions",
                sql: "\"BalanceBefore\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bets_ActualPayout_NonNegative",
                table: "Bets",
                sql: "\"ActualPayout\" IS NULL OR \"ActualPayout\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bets_CombinedOdds_MinimumOne",
                table: "Bets",
                sql: "\"CombinedOddsDecimal\" >= 1.0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bets_PotentialPayout_NonNegative",
                table: "Bets",
                sql: "\"PotentialPayout\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bets_Stake_Positive",
                table: "Bets",
                sql: "\"Stake\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_TotalBet_NonNegative",
                table: "Wallets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_TotalDeposited_NonNegative",
                table: "Wallets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_TotalWithdrawn_NonNegative",
                table: "Wallets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wallets_TotalWon_NonNegative",
                table: "Wallets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_Amount_Positive",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_BalanceAfter_NonNegative",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transactions_BalanceBefore_NonNegative",
                table: "Transactions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bets_ActualPayout_NonNegative",
                table: "Bets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bets_CombinedOdds_MinimumOne",
                table: "Bets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bets_PotentialPayout_NonNegative",
                table: "Bets");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bets_Stake_Positive",
                table: "Bets");
        }
    }
}
