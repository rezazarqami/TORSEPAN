using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TORSEPAN.Infrastructure.Migrations { public partial class AddOfflineReleaseCompatibility : Migration {
protected override void Up(MigrationBuilder migrationBuilder) {
migrationBuilder.AddColumn<bool>(name: "IsPayrollExcluded", table: "ProductionEvents", type: "boolean", nullable: false, defaultValue: false);
migrationBuilder.AddColumn<DateTime>(name: "PayrollExcludedAtUtc", table: "ProductionEvents", type: "timestamp with time zone", nullable: true);
migrationBuilder.AddColumn<Guid>(name: "PayrollExcludedByUserId", table: "ProductionEvents", type: "uuid", nullable: true);
migrationBuilder.AddColumn<string>(name: "BuyerPhoneNumber", table: "Handpans", type: "text", nullable: true);
migrationBuilder.AddColumn<bool>(name: "IsExportSale", table: "Handpans", type: "boolean", nullable: true);
migrationBuilder.AddColumn<Guid>(name: "SaleLeadSourceId", table: "Handpans", type: "uuid", nullable: true);
migrationBuilder.AddColumn<Guid>(name: "SalesReferrerId", table: "Handpans", type: "uuid", nullable: true);
migrationBuilder.AddColumn<DateTime>(name: "WarrantyActivatedAt", table: "Handpans", type: "timestamp with time zone", nullable: true);
migrationBuilder.CreateTable(name: "SaleLeadSources", columns: table => new {
 Id = table.Column<Guid>(type: "uuid", nullable: false),
 Name = table.Column<string>(type: "text", nullable: false),
 RequiresReferrer = table.Column<bool>(type: "boolean", nullable: false),
 IsActive = table.Column<bool>(type: "boolean", nullable: false)
 }, constraints: table => { table.PrimaryKey("PK_SaleLeadSources", x => x.Id); });
migrationBuilder.CreateTable(name: "SalesReferrers", columns: table => new {
 Id = table.Column<Guid>(type: "uuid", nullable: false),
 Name = table.Column<string>(type: "text", nullable: false),
 
 IsActive = table.Column<bool>(type: "boolean", nullable: false)
 }, constraints: table => { table.PrimaryKey("PK_SalesReferrers", x => x.Id); });
}
protected override void Down(MigrationBuilder migrationBuilder) {
migrationBuilder.DropColumn(name: "IsPayrollExcluded", table: "ProductionEvents");
migrationBuilder.DropColumn(name: "PayrollExcludedAtUtc", table: "ProductionEvents");
migrationBuilder.DropColumn(name: "PayrollExcludedByUserId", table: "ProductionEvents");
migrationBuilder.DropColumn(name: "BuyerPhoneNumber", table: "Handpans");
migrationBuilder.DropColumn(name: "IsExportSale", table: "Handpans");
migrationBuilder.DropColumn(name: "SaleLeadSourceId", table: "Handpans");
migrationBuilder.DropColumn(name: "SalesReferrerId", table: "Handpans");
migrationBuilder.DropColumn(name: "WarrantyActivatedAt", table: "Handpans");
migrationBuilder.DropTable(name: "SaleLeadSources");
migrationBuilder.DropTable(name: "SalesReferrers");
}
}}