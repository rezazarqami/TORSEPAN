using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260814030000_AddBowlScaleAndPayrollScale")]
public partial class AddBowlScaleAndPayrollScale : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "ScaleId", table: "Bowls", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "ScaleId", table: "PayrollRates", type: "uuid", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Bowls_ScaleId", table: "Bowls", column: "ScaleId");
        migrationBuilder.CreateIndex(name: "IX_PayrollRates_ScaleId", table: "PayrollRates", column: "ScaleId");
        migrationBuilder.AddForeignKey(name: "FK_Bowls_Scales_ScaleId", table: "Bowls", column: "ScaleId", principalTable: "Scales", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        migrationBuilder.AddForeignKey(name: "FK_PayrollRates_Scales_ScaleId", table: "PayrollRates", column: "ScaleId", principalTable: "Scales", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Bowls_Scales_ScaleId", table: "Bowls");
        migrationBuilder.DropForeignKey(name: "FK_PayrollRates_Scales_ScaleId", table: "PayrollRates");
        migrationBuilder.DropIndex(name: "IX_Bowls_ScaleId", table: "Bowls");
        migrationBuilder.DropIndex(name: "IX_PayrollRates_ScaleId", table: "PayrollRates");
        migrationBuilder.DropColumn(name: "ScaleId", table: "Bowls");
        migrationBuilder.DropColumn(name: "ScaleId", table: "PayrollRates");
    }
}
