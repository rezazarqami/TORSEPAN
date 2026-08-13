using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260813010000_ProductionNotesSalesAndMaterials")]
public partial class ProductionNotesSalesAndMaterials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "SalePrice", table: "Handpans", type: "numeric(18,2)", nullable: true);
        migrationBuilder.AddColumn<string>(name: "SaleDestination", table: "Handpans", type: "character varying(200)", maxLength: 200, nullable: true);
        migrationBuilder.Sql("""
            UPDATE "Materials" m SET "TopBowlQuantity" = m."TopBowlQuantity" + 1
            FROM "Bowls" b WHERE b."ProductionCode" = '۱۲۸' AND b."BowlType" = 1 AND b."MaterialId" = m."Id";
            UPDATE "Materials" m SET "BottomBowlQuantity" = m."BottomBowlQuantity" + 1
            FROM "Bowls" b WHERE b."ProductionCode" = '۱۲۸' AND b."BowlType" = 2 AND b."MaterialId" = m."Id";
            DELETE FROM "ProductionEvents" WHERE "BowlId" IN (SELECT "Id" FROM "Bowls" WHERE "ProductionCode" = '۱۲۸');
            DELETE FROM "Bowls" WHERE "ProductionCode" = '۱۲۸';
            UPDATE "Bowls" SET "ProductionCode" = translate("ProductionCode", '۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', '01234567890123456789');
            UPDATE "Materials" SET "Name" = 'Ember Steel A' WHERE lower(trim("Name")) = 'ember steel';
            INSERT INTO "Materials" ("Id", "Name", "Category", "Quantity", "TopBowlQuantity", "BottomBowlQuantity", "LowStockThreshold", "TopBowlLowStockThreshold", "BottomBowlLowStockThreshold")
            SELECT gen_random_uuid(), 'Ember Steel B', 4, 0, 0, 0, 0, 0, 0
            WHERE NOT EXISTS (SELECT 1 FROM "Materials" WHERE lower(trim("Name")) = 'ember steel b');
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SalePrice", table: "Handpans");
        migrationBuilder.DropColumn(name: "SaleDestination", table: "Handpans");
    }
}
