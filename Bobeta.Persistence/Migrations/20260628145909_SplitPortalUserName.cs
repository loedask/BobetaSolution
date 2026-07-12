using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bobeta.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitPortalUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "PortalUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "PortalUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "PortalUsers"
                SET "FirstName" = "DisplayName"
                WHERE "FirstName" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "PortalUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "PortalUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "PortalUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "PortalUsers"
                SET "DisplayName" = TRIM("FirstName" || ' ' || "LastName")
                WHERE "DisplayName" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "DisplayName",
                table: "PortalUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "PortalUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "PortalUsers");
        }
    }
}
