using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommissionTier",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<DateTime>(
                name: "CommissionTierLastUpdated",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BackBetCommission",
                table: "BetMatches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LayBetCommission",
                table: "BetMatches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MakerBetId",
                table: "BetMatches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TakerBetId",
                table: "BetMatches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "UserStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalVolumeAllTime = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalBetsAllTime = table.Column<int>(type: "integer", nullable: false),
                    TotalMatchesAllTime = table.Column<int>(type: "integer", nullable: false),
                    TotalCommissionPaidAllTime = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume30Day = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Bets30Day = table.Column<int>(type: "integer", nullable: false),
                    Matches30Day = table.Column<int>(type: "integer", nullable: false),
                    Commission30Day = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume7Day = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Bets7Day = table.Column<int>(type: "integer", nullable: false),
                    MakerTradesAllTime = table.Column<int>(type: "integer", nullable: false),
                    TakerTradesAllTime = table.Column<int>(type: "integer", nullable: false),
                    MakerVolumeAllTime = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TakerVolumeAllTime = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetProfitAllTime = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LargestWin = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LargestLoss = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LastBetPlaced = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastBetSettled = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStatistics_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserStatistics_LastUpdated",
                table: "UserStatistics",
                column: "LastUpdated");

            migrationBuilder.CreateIndex(
                name: "IX_UserStatistics_UserId",
                table: "UserStatistics",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStatistics_Volume30Day",
                table: "UserStatistics",
                column: "Volume30Day");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserStatistics");

            migrationBuilder.DropColumn(
                name: "CommissionTier",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CommissionTierLastUpdated",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BackBetCommission",
                table: "BetMatches");

            migrationBuilder.DropColumn(
                name: "LayBetCommission",
                table: "BetMatches");

            migrationBuilder.DropColumn(
                name: "MakerBetId",
                table: "BetMatches");

            migrationBuilder.DropColumn(
                name: "TakerBetId",
                table: "BetMatches");
        }
    }
}
