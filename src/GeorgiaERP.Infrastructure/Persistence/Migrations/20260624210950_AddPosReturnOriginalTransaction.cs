using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeorgiaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosReturnOriginalTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OriginalTransactionId",
                schema: "public",
                table: "pos_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_pos_transactions_original",
                schema: "public",
                table: "pos_transactions",
                column: "OriginalTransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pos_transactions_original",
                schema: "public",
                table: "pos_transactions");

            migrationBuilder.DropColumn(
                name: "OriginalTransactionId",
                schema: "public",
                table: "pos_transactions");
        }
    }
}
