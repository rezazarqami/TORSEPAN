using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260824020000_NormalizeTwoNoteBottomBowlMaterial")]
public partial class NormalizeTwoNoteBottomBowlMaterial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "Bowls" AS b
            SET "MaterialId" = material."Id"
            FROM "Scales" AS scale
            CROSS JOIN LATERAL (
                SELECT "Id"
                FROM "Materials"
                WHERE LOWER(TRIM("Name")) = LOWER('Stainless Steel')
                ORDER BY "Id"
                LIMIT 1
            ) AS material
            WHERE b."ScaleId" = scale."Id"
              AND b."BowlType" = 2
              AND LOWER(REPLACE(REPLACE(TRIM(scale."Name"), '–', '-'), '—', '-')) = LOWER('2 نت زیر F-G')
              AND b."MaterialId" <> material."Id";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This is a data correction; the former incorrect material cannot be inferred safely.
    }
}
