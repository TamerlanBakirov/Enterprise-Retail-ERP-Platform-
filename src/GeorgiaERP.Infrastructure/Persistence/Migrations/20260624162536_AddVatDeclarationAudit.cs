using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeorgiaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVatDeclarationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "public",
                table: "vat_declarations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SubmittedBy",
                schema: "public",
                table: "vat_declarations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "public",
                table: "vat_declarations");

            migrationBuilder.DropColumn(
                name: "SubmittedBy",
                schema: "public",
                table: "vat_declarations");
        }
    }
}
