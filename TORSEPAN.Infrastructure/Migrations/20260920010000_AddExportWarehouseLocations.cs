using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260920010000_AddExportWarehouseLocations")]
public partial class AddExportWarehouseLocations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "ExportWarehouseLocation", table: "Bowls", type: "integer", nullable: true);
        migrationBuilder.AddColumn<int>(name: "ExportWarehouseLocation", table: "Handpans", type: "integer", nullable: true);
        migrationBuilder.Sql("UPDATE \"Bowls\" SET \"ExportWarehouseLocation\" = 1 WHERE \"Stage\" = 22");
        migrationBuilder.Sql("UPDATE \"Handpans\" SET \"ExportWarehouseLocation\" = 1 WHERE \"Stage\" = 22");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ExportWarehouseLocation", table: "Bowls");
        migrationBuilder.DropColumn(name: "ExportWarehouseLocation", table: "Handpans");
    }
}
