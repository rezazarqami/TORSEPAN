using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260825010000_AddNitrideBowlMaterial")]
public partial class AddNitrideBowlMaterial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "Materials"
            SET "Name" = 'NITRIDE', "Category" = 4
            WHERE lower(replace(trim("Name"), ' ', '')) IN ('nitride', 'nitridedsteel')
               OR trim("Name") = 'نیتراید';

            INSERT INTO "Materials" (
                "Id", "Name", "Category", "Quantity", "TopBowlQuantity", "BottomBowlQuantity",
                "LowStockThreshold", "TopBowlLowStockThreshold", "BottomBowlLowStockThreshold",
                "TopBowlCodeTemplate", "BottomBowlCodeTemplate")
            SELECT
                '10000000-0000-0000-0000-000000000005', 'NITRIDE', 4, 0, 0, 0, 0, 0, 0, '', ''
            WHERE NOT EXISTS (
                SELECT 1 FROM "Materials"
                WHERE lower(replace(trim("Name"), ' ', '')) IN ('nitride', 'nitridedsteel')
                   OR trim("Name") = 'نیتراید'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "Materials"
            WHERE "Id" = '10000000-0000-0000-0000-000000000005'
              AND "Quantity" = 0
              AND "TopBowlQuantity" = 0
              AND "BottomBowlQuantity" = 0
              AND NOT EXISTS (
                  SELECT 1 FROM "Bowls" WHERE "Bowls"."MaterialId" = "Materials"."Id"
              );
            """);
    }
}
