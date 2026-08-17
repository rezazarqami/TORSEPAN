using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260818010000_CategorizeScales")]
public partial class CategorizeScales : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Usage",
            table: "Scales",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.Sql("""
            UPDATE "Scales" AS s
            SET "Usage" =
                (CASE WHEN EXISTS (SELECT 1 FROM "Bowls" b WHERE b."ScaleId" = s."Id" AND b."BowlType" = 1) THEN 1 ELSE 0 END) |
                (CASE WHEN EXISTS (SELECT 1 FROM "Bowls" b WHERE b."ScaleId" = s."Id" AND b."BowlType" = 2) THEN 2 ELSE 0 END) |
                (CASE WHEN EXISTS (SELECT 1 FROM "Handpans" h WHERE h."ScaleId" = s."Id") THEN 4 ELSE 0 END)
            """);
        migrationBuilder.Sql("UPDATE \"Scales\" SET \"Usage\" = 7 WHERE \"Usage\" = 0;");
        migrationBuilder.AlterColumn<int>(
            name: "Usage",
            table: "Scales",
            type: "integer",
            nullable: false,
            defaultValue: 7,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "Usage", table: "Scales");
}
