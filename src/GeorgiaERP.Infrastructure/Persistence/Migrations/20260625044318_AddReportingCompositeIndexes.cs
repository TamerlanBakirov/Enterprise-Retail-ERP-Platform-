using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeorgiaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_product_barcodes_product_primary",
                schema: "public",
                table: "product_barcodes",
                columns: new[] { "ProductId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_transactions_status_date",
                schema: "public",
                table: "pos_transactions",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_status_entry_date",
                schema: "public",
                table: "journal_entries",
                columns: new[] { "Status", "EntryDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_product_barcodes_product_primary",
                schema: "public",
                table: "product_barcodes");

            migrationBuilder.DropIndex(
                name: "IX_pos_transactions_status_date",
                schema: "public",
                table: "pos_transactions");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_status_entry_date",
                schema: "public",
                table: "journal_entries");
        }
    }
}
