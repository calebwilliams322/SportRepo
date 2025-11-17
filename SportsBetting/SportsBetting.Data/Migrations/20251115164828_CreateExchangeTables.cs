using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateExchangeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsensusOdds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutcomeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AverageOdds = table.Column<decimal>(type: "numeric", nullable: false),
                    MinOdds = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxOdds = table.Column<decimal>(type: "numeric", nullable: false),
                    SampleSize = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsensusOdds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsensusOdds_Outcomes_OutcomeId",
                        column: x => x.OutcomeId,
                        principalTable: "Outcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeBets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Side = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ProposedOdds = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalStake = table.Column<decimal>(type: "numeric", nullable: false),
                    MatchedStake = table.Column<decimal>(type: "numeric", nullable: false),
                    UnmatchedStake = table.Column<decimal>(type: "numeric", nullable: false),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeBets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeBets_Bets_BetId",
                        column: x => x.BetId,
                        principalTable: "Bets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BetMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BackBetId = table.Column<Guid>(type: "uuid", nullable: false),
                    LayBetId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchedStake = table.Column<decimal>(type: "numeric", nullable: false),
                    MatchedOdds = table.Column<decimal>(type: "numeric", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSettled = table.Column<bool>(type: "boolean", nullable: false),
                    WinnerBetId = table.Column<Guid>(type: "uuid", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BetMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BetMatches_ExchangeBets_BackBetId",
                        column: x => x.BackBetId,
                        principalTable: "ExchangeBets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BetMatches_ExchangeBets_LayBetId",
                        column: x => x.LayBetId,
                        principalTable: "ExchangeBets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BetMatches_BackBetId",
                table: "BetMatches",
                column: "BackBetId");

            migrationBuilder.CreateIndex(
                name: "IX_BetMatches_IsSettled",
                table: "BetMatches",
                column: "IsSettled",
                filter: "\"IsSettled\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BetMatches_LayBetId",
                table: "BetMatches",
                column: "LayBetId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsensusOdds_OutcomeId_ExpiresAt",
                table: "ConsensusOdds",
                columns: new[] { "OutcomeId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeBets_BetId",
                table: "ExchangeBets",
                column: "BetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeBets_State",
                table: "ExchangeBets",
                column: "State",
                filter: "\"State\" IN ('Unmatched', 'PartiallyMatched')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BetMatches");

            migrationBuilder.DropTable(
                name: "ConsensusOdds");

            migrationBuilder.DropTable(
                name: "ExchangeBets");
        }
    }
}
