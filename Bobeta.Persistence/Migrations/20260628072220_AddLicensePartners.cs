using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bobeta.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLicensePartners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Players",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LicensePartnerId",
                table: "GameResults",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PartnerCommission",
                table: "GameResults",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "LicensePartners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PortalUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensePartners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicensePartners_PortalUsers_PortalUserId",
                        column: x => x.PortalUserId,
                        principalTable: "PortalUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LicensePartnerCountryAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicensePartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensePartnerCountryAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicensePartnerCountryAssignments_LicensePartners_LicensePar~",
                        column: x => x.LicensePartnerId,
                        principalTable: "LicensePartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RevenueAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    LicensePartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrossPlatformRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PartnerSharePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PartnerAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PlatformRetainedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevenueAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RevenueAllocations_LicensePartners_LicensePartnerId",
                        column: x => x.LicensePartnerId,
                        principalTable: "LicensePartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LicensePartnerRevenueShareRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevenueSharePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByPortalUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensePartnerRevenueShareRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicensePartnerRevenueShareRates_LicensePartnerCountryAssign~",
                        column: x => x.AssignmentId,
                        principalTable: "LicensePartnerCountryAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePartnerCountryAssignments_CountryCode_IsActive",
                table: "LicensePartnerCountryAssignments",
                columns: new[] { "CountryCode", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePartnerCountryAssignments_LicensePartnerId_CountryCo~",
                table: "LicensePartnerCountryAssignments",
                columns: new[] { "LicensePartnerId", "CountryCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LicensePartnerRevenueShareRates_AssignmentId_EffectiveFrom",
                table: "LicensePartnerRevenueShareRates",
                columns: new[] { "AssignmentId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePartners_PortalUserId",
                table: "LicensePartners",
                column: "PortalUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RevenueAllocations_LicensePartnerId",
                table: "RevenueAllocations",
                column: "LicensePartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RevenueAllocations_SourceType_SourceId",
                table: "RevenueAllocations",
                columns: new[] { "SourceType", "SourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicensePartnerRevenueShareRates");

            migrationBuilder.DropTable(
                name: "RevenueAllocations");

            migrationBuilder.DropTable(
                name: "LicensePartnerCountryAssignments");

            migrationBuilder.DropTable(
                name: "LicensePartners");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "LicensePartnerId",
                table: "GameResults");

            migrationBuilder.DropColumn(
                name: "PartnerCommission",
                table: "GameResults");
        }
    }
}
