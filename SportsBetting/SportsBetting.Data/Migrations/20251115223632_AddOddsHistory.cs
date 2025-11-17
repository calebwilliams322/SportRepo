using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOddsHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OddsHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutcomeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RawBookmakerData = table.Column<string>(type: "jsonb", nullable: true),
                    Odds = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OddsHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OddsHistory_Outcomes_OutcomeId",
                        column: x => x.OutcomeId,
                        principalTable: "Outcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OddsHistory_OutcomeId",
                table: "OddsHistory",
                column: "OutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_OddsHistory_OutcomeId_Timestamp",
                table: "OddsHistory",
                columns: new[] { "OutcomeId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_OddsHistory_Timestamp",
                table: "OddsHistory",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OddsHistory");
        }
    }
}
