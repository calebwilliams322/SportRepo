using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeCommissionRate",
                table: "Markets",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "Markets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BetMode",
                table: "Bets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ProposedOdds",
                table: "Bets",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "Bets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExchangeCommissionRate",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "BetMode",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "ProposedOdds",
                table: "Bets");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Bets");
        }
    }
}
