using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TORSEPAN.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentQualityWarehouses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QualityWarehouseId",
                table: "Handpans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "InstrumentWarehouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentWarehouses", x => x.Id);
                    table.CheckConstraint("CK_InstrumentWarehouses_Three", "\"Id\" BETWEEN 1 AND 3");
                });

            migrationBuilder.InsertData(
                table: "InstrumentWarehouses",
                columns: new[] { "Id", "Name", "Version" },
                values: new object[,]
                {
                    { 1, "Economy", new Guid("11111111-1111-4111-8111-111111111111") },
                    { 2, "Studio", new Guid("22222222-2222-4222-8222-222222222222") },
                    { 3, "Signature", new Guid("33333333-3333-4333-8333-333333333333") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Handpans_QualityWarehouseId",
                table: "Handpans",
                column: "QualityWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentWarehouses_Name",
                table: "InstrumentWarehouses",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Handpans_InstrumentWarehouses_QualityWarehouseId",
                table: "Handpans",
                column: "QualityWarehouseId",
                principalTable: "InstrumentWarehouses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Handpans_InstrumentWarehouses_QualityWarehouseId",
                table: "Handpans");

            migrationBuilder.DropTable(
                name: "InstrumentWarehouses");

            migrationBuilder.DropIndex(
                name: "IX_Handpans_QualityWarehouseId",
                table: "Handpans");

            migrationBuilder.DropColumn(
                name: "QualityWarehouseId",
                table: "Handpans");
        }
    }
}
