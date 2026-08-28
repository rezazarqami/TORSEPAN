using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext)), Migration("20260828020000_AddDesignExportRates")]
public partial class AddDesignExportRates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<decimal>(
        name: "ExportRate", table: "DesignTypes", type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m);
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "ExportRate", table: "DesignTypes");
}
