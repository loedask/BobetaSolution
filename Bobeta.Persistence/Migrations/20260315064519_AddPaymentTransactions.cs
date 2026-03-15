using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bobeta.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MoMoTransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ExternalReference",
                table: "PaymentTransactions",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_MoMoTransactionId",
                table: "PaymentTransactions",
                column: "MoMoTransactionId",
                filter: "\"MoMoTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PlayerId",
                table: "PaymentTransactions",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactions");
        }
    }
}
