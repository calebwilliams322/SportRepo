using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOddsApiExternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Outcomes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOddsUpdate",
                table: "Outcomes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Markets",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Events",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Outcomes_ExternalId",
                table: "Outcomes",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Outcomes_LastOddsUpdate",
                table: "Outcomes",
                column: "LastOddsUpdate");

            migrationBuilder.CreateIndex(
                name: "IX_Markets_ExternalId",
                table: "Markets",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ExternalId",
                table: "Events",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_LastSyncedAt",
                table: "Events",
                column: "LastSyncedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Outcomes_ExternalId",
                table: "Outcomes");

            migrationBuilder.DropIndex(
                name: "IX_Outcomes_LastOddsUpdate",
                table: "Outcomes");

            migrationBuilder.DropIndex(
                name: "IX_Markets_ExternalId",
                table: "Markets");

            migrationBuilder.DropIndex(
                name: "IX_Events_ExternalId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_LastSyncedAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Outcomes");

            migrationBuilder.DropColumn(
                name: "LastOddsUpdate",
                table: "Outcomes");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "Events");
        }
    }
}
