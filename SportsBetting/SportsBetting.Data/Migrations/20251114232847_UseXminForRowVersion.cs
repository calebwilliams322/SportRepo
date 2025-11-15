using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsBetting.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseXminForRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old RowVersion column - we'll use PostgreSQL's xmin system column instead
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Wallets");

            // Note: xmin is a system column that already exists in PostgreSQL
            // We don't need to create it, just map to it in EF Core configuration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the RowVersion column if rolling back
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Wallets",
                type: "bytea",
                rowVersion: true,
                nullable: true);
        }
    }
}
