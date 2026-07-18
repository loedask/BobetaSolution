using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bobeta.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerLastSeenOnline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenOnlineUtc",
                table: "Players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_LastSeenOnlineUtc",
                table: "Players",
                column: "LastSeenOnlineUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_LastSeenOnlineUtc",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "LastSeenOnlineUtc",
                table: "Players");
        }
    }
}
