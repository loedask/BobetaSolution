using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bobeta.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInfluencers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InfluencerAmount",
                table: "RevenueAllocations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CreatorChargedAmount",
                table: "GameSessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql("""UPDATE "GameSessions" SET "CreatorChargedAmount" = "BetAmount" WHERE "CreatorChargedAmount" = 0;""");

            migrationBuilder.Sql("""
INSERT INTO "PlatformSettings" ("Key", "Value", "UpdatedAt", "UpdatedByPortalUserId")
VALUES ('InfluencerPlayerDiscountPercent', '5', NOW() AT TIME ZONE 'utc', NULL)
ON CONFLICT ("Key") DO NOTHING;
""");

            migrationBuilder.AddColumn<decimal>(
                name: "OpponentChargedAmount",
                table: "GameSessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InfluencerCommission",
                table: "GameResults",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Influencers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PortalUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Influencers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Influencers_PortalUsers_PortalUserId",
                        column: x => x.PortalUserId,
                        principalTable: "PortalUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByPortalUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "InfluencerCodeRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InfluencerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GameSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfluencerCodeRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfluencerCodeRedemptions_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InfluencerCodeRedemptions_Influencers_InfluencerId",
                        column: x => x.InfluencerId,
                        principalTable: "Influencers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InfluencerCodeRedemptions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InfluencerCommissionAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InfluencerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrossPlatformRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AttributionBase = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    InfluencerAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfluencerCommissionAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfluencerCommissionAllocations_GameSessions_GameSessionId",
                        column: x => x.GameSessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InfluencerCommissionAllocations_Influencers_InfluencerId",
                        column: x => x.InfluencerId,
                        principalTable: "Influencers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InfluencerCommissionAllocations_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerCodeRedemptions_GameSessionId",
                table: "InfluencerCodeRedemptions",
                column: "GameSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerCodeRedemptions_InfluencerId_PlayerId",
                table: "InfluencerCodeRedemptions",
                columns: new[] { "InfluencerId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerCodeRedemptions_PlayerId_GameSessionId",
                table: "InfluencerCodeRedemptions",
                columns: new[] { "PlayerId", "GameSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerCommissionAllocations_GameSessionId_PlayerId",
                table: "InfluencerCommissionAllocations",
                columns: new[] { "GameSessionId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerCommissionAllocations_InfluencerId",
                table: "InfluencerCommissionAllocations",
                column: "InfluencerId");

            migrationBuilder.CreateIndex(
                name: "IX_InfluencerCommissionAllocations_PlayerId",
                table: "InfluencerCommissionAllocations",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Influencers_Code",
                table: "Influencers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Influencers_PortalUserId",
                table: "Influencers",
                column: "PortalUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InfluencerCodeRedemptions");

            migrationBuilder.DropTable(
                name: "InfluencerCommissionAllocations");

            migrationBuilder.DropTable(
                name: "PlatformSettings");

            migrationBuilder.DropTable(
                name: "Influencers");

            migrationBuilder.DropColumn(
                name: "InfluencerAmount",
                table: "RevenueAllocations");

            migrationBuilder.DropColumn(
                name: "CreatorChargedAmount",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "OpponentChargedAmount",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "InfluencerCommission",
                table: "GameResults");
        }
    }
}
