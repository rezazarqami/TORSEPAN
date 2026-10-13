using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20261013010000_CompactBowlCodeTemplates")]
public sealed class CompactBowlCodeTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        UPDATE "Materials" SET "TopBowlCodeTemplate" = left("TopBowlCodeTemplate", 1) || '10000'
        WHERE "Category" = 4 AND "TopBowlCodeTemplate" ~ '^[A-Z]S-00000$';
        UPDATE "Materials" SET "BottomBowlCodeTemplate" = left("BottomBowlCodeTemplate", 1) || '20000'
        WHERE "Category" = 4 AND "BottomBowlCodeTemplate" ~ '^[A-Z]G-00000$';
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        UPDATE "Materials" SET "TopBowlCodeTemplate" = left("TopBowlCodeTemplate", 1) || 'S-00000'
        WHERE "Category" = 4 AND "TopBowlCodeTemplate" ~ '^[A-Z]10000$';
        UPDATE "Materials" SET "BottomBowlCodeTemplate" = left("BottomBowlCodeTemplate", 1) || 'G-00000'
        WHERE "Category" = 4 AND "BottomBowlCodeTemplate" ~ '^[A-Z]20000$';
        """);
}
