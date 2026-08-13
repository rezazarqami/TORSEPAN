using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260814010000_AddPayrollAndUserOrder")]
public partial class AddPayrollAndUserOrder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "DisplayOrder", table: "Users", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.CreateTable(name: "PayrollRates", columns: table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Action = table.Column<int>(type: "integer", nullable: false),
            MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
            BowlType = table.Column<int>(type: "integer", nullable: true),
            Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_PayrollRates", x => x.Id);
            table.ForeignKey(name: "FK_PayrollRates_Materials_MaterialId", column: x => x.MaterialId, principalTable: "Materials", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex(name: "IX_PayrollRates_MaterialId", table: "PayrollRates", column: "MaterialId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PayrollRates");
        migrationBuilder.DropColumn(name: "DisplayOrder", table: "Users");
    }
}
