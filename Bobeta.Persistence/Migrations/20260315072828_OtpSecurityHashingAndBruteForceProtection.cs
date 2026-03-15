using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bobeta.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OtpSecurityHashingAndBruteForceProtection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "OtpCodes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<int>(
                name: "FailedAttemptCount",
                table: "OtpCodes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "OtpCodes",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedAttemptCount",
                table: "OtpCodes");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "OtpCodes");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "OtpCodes",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);
        }
    }
}
