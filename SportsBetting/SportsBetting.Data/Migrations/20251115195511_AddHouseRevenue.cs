using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseRevenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HouseRevenue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SportsbookGrossRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    SportsbookPayouts = table.Column<decimal>(type: "numeric", nullable: false),
                    SportsbookNetRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    SportsbookBetsSettled = table.Column<int>(type: "integer", nullable: false),
                    SportsbookVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    ExchangeCommissionRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    ExchangeMatchesSettled = table.Column<int>(type: "integer", nullable: false),
                    ExchangeVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    ExchangeCustomerPayouts = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalVolume = table.Column<decimal>(type: "numeric", nullable: false),
                    EffectiveMargin = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseRevenue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HouseRevenue_PeriodStart",
                table: "HouseRevenue",
                column: "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_HouseRevenue_PeriodStart_PeriodEnd_PeriodType",
                table: "HouseRevenue",
                columns: new[] { "PeriodStart", "PeriodEnd", "PeriodType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HouseRevenue");
        }
    }
}
