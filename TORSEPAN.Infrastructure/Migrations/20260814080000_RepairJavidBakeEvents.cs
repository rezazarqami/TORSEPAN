using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260814080000_RepairJavidBakeEvents")]
public partial class RepairJavidBakeEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Javid has only performed furnace/bake work. Historical versions stored
        // each of those bowl events as Glue, making 40 bowl events appear as 20
        // glued handpans on the dashboard.
        migrationBuilder.Sql("""
            UPDATE "ProductionEvents" AS e
            SET "Action" = 4
            FROM "Users" AS u
            WHERE e."UserId" = u."Id"
              AND e."Action" = 5
              AND (u."FullName" ILIKE '%جاوید%' OR u."UserName" ILIKE '%javid%' OR u."UserName" ILIKE '%javied%');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
