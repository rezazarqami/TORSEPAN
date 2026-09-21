using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260914170000_AddProductionEventPayrollExclusion")]
public partial class AddProductionEventPayrollExclusion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "IsPayrollExcluded", table: "ProductionEvents", type: "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<Guid>(name: "PayrollExcludedByUserId", table: "ProductionEvents", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "PayrollExcludedAtUtc", table: "ProductionEvents", type: "timestamp with time zone", nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsPayrollExcluded", table: "ProductionEvents");
        migrationBuilder.DropColumn(name: "PayrollExcludedByUserId", table: "ProductionEvents");
        migrationBuilder.DropColumn(name: "PayrollExcludedAtUtc", table: "ProductionEvents");
    }
}
