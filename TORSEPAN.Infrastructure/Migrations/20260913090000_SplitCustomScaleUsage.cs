using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260913090000_SplitCustomScaleUsage")]
public partial class SplitCustomScaleUsage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE \"Scales\" SET \"Usage\" = 56 WHERE \"Usage\" = 8;");
        migrationBuilder.AlterColumn<int>(
            name: "Usage",
            table: "Scales",
            type: "integer",
            nullable: false,
            defaultValue: 63,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 15);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE \"Scales\" SET \"Usage\" = 8 WHERE (\"Usage\" & 56) <> 0;");
        migrationBuilder.AlterColumn<int>(
            name: "Usage",
            table: "Scales",
            type: "integer",
            nullable: false,
            defaultValue: 15,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 63);
    }
}
