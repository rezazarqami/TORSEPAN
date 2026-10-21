using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesProfitSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetProfit",
                table: "Handpans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfitCurrency",
                table: "Handpans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "SaleCurrency",
                table: "Handpans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Currency",
                table: "AccountingDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "SaleProfitEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandpanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    RolePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    NetProfit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    SoldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReversedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleProfitEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SaleProfitRules",
                columns: table => new
                {
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleProfitRules", x => x.Action);
                });

            migrationBuilder.InsertData(
                table: "SaleProfitRules",
                columns: new[] { "Action", "Percent", "Version" },
                values: new object[,]
                {
                    { 2, 2m, new Guid("00000000-0000-4000-8000-000000000002") },
                    { 3, 7m, new Guid("00000000-0000-4000-8000-000000000003") },
                    { 5, 1m, new Guid("00000000-0000-4000-8000-000000000005") },
                    { 6, 13m, new Guid("00000000-0000-4000-8000-000000000006") },
                    { 7, 13m, new Guid("00000000-0000-4000-8000-000000000007") },
                    { 13, 0m, new Guid("00000000-0000-4000-8000-000000000013") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleProfitEntries_HandpanId_ReversedAt",
                table: "SaleProfitEntries",
                columns: new[] { "HandpanId", "ReversedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleProfitEntries_UserId_Currency",
                table: "SaleProfitEntries",
                columns: new[] { "UserId", "Currency" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleProfitEntries");

            migrationBuilder.DropTable(
                name: "SaleProfitRules");

            migrationBuilder.DropColumn(
                name: "NetProfit",
                table: "Handpans");

            migrationBuilder.DropColumn(
                name: "ProfitCurrency",
                table: "Handpans");

            migrationBuilder.DropColumn(
                name: "SaleCurrency",
                table: "Handpans");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "AccountingDocuments");
        }
    }
}
