using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260807000200_RemoveUnwantedDefaultMaterials")]
public partial class RemoveUnwantedDefaultMaterials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "Materials"
            WHERE "Id" IN (
                '10000000-0000-0000-0000-000000000001',
                '10000000-0000-0000-0000-000000000002',
                '10000000-0000-0000-0000-000000000003',
                '10000000-0000-0000-0000-000000000004'
            )
            AND NOT EXISTS (
                SELECT 1 FROM "Bowls"
                WHERE "Bowls"."MaterialId" = "Materials"."Id"
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO "Materials" ("Id", "Name") VALUES
                ('10000000-0000-0000-0000-000000000001', 'EmberSteel'),
                ('10000000-0000-0000-0000-000000000002', 'StainlessSteel'),
                ('10000000-0000-0000-0000-000000000003', 'NitridedSteel'),
                ('10000000-0000-0000-0000-000000000004', 'Custom')
            ON CONFLICT ("Name") DO NOTHING;
            """);
    }
}
