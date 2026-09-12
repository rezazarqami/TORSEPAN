using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260912120000_AddCustomScaleWorkflow")]
public partial class AddCustomScaleWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsCustomScale",
            table: "Bowls",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsCustom",
            table: "PayrollRates",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AlterColumn<int>(
            name: "Usage",
            table: "Scales",
            type: "integer",
            nullable: false,
            defaultValue: 15,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 7);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsCustomScale", table: "Bowls");
        migrationBuilder.DropColumn(name: "IsCustom", table: "PayrollRates");
        migrationBuilder.AlterColumn<int>(
            name: "Usage",
            table: "Scales",
            type: "integer",
            nullable: false,
            defaultValue: 7,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 15);
    }
}
