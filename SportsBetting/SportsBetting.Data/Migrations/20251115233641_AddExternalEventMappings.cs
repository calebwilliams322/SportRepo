using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalEventMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalEventMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalEventMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalEventMappings_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEventMappings_EventId",
                table: "ExternalEventMappings",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEventMappings_ExternalId_Provider",
                table: "ExternalEventMappings",
                columns: new[] { "ExternalId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEventMappings_Provider",
                table: "ExternalEventMappings",
                column: "Provider");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalEventMappings");
        }
    }
}
