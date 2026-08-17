using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260818030000_AuditAndRepairMissingBowlScales")]
public partial class AuditAndRepairMissingBowlScales : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // These two groups intentionally have no scale: bowls waiting for dimple,
        // and bottom bowls without notes. Repair every other legacy missing value
        // from the most frequently used existing scale for the same bowl type.
        migrationBuilder.Sql("""
            UPDATE "Bowls" AS target
            SET "ScaleId" = (
                SELECT source."ScaleId"
                FROM "Bowls" AS source
                INNER JOIN "Scales" AS scale ON scale."Id" = source."ScaleId"
                WHERE source."BowlType" = target."BowlType"
                  AND source."ScaleId" IS NOT NULL
                GROUP BY source."ScaleId"
                ORDER BY COUNT(*) DESC
                LIMIT 1
            )
            WHERE target."ScaleId" IS NULL
              AND target."Stage" <> 2
              AND NOT (target."BowlType" = 2 AND target."HasNotes" = FALSE)
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) { }
}
