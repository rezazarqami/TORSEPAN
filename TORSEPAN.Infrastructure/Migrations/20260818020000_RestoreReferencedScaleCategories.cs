using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260818020000_RestoreReferencedScaleCategories")]
public partial class RestoreReferencedScaleCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Scales" AS s
            SET "Usage" = s."Usage" |
                (CASE WHEN EXISTS (SELECT 1 FROM "Bowls" b WHERE b."ScaleId" = s."Id" AND b."BowlType" = 1) THEN 1 ELSE 0 END) |
                (CASE WHEN EXISTS (SELECT 1 FROM "Bowls" b WHERE b."ScaleId" = s."Id" AND b."BowlType" = 2) THEN 2 ELSE 0 END) |
                (CASE WHEN EXISTS (SELECT 1 FROM "Handpans" h WHERE h."ScaleId" = s."Id") THEN 4 ELSE 0 END),
                "IsActive" = TRUE
            WHERE EXISTS (SELECT 1 FROM "Bowls" b WHERE b."ScaleId" = s."Id")
               OR EXISTS (SELECT 1 FROM "Handpans" h WHERE h."ScaleId" = s."Id")
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
