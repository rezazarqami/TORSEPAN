using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260907010000_AddSalesAdminRole")]
public partial class AddSalesAdminRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "Roles" ("Id", "Name", "DisplayName")
            VALUES ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'SalesAdmin', 'ادمین فروش')
            ON CONFLICT ("Name") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM \"Roles\" WHERE \"Name\" = 'SalesAdmin';");
    }
}
