using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260824030000_AddMarketerRole")]
public partial class AddMarketerRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "Roles" ("Id", "Name", "DisplayName")
            VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Marketer', 'بازاریاب')
            ON CONFLICT ("Name") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM \"Roles\" WHERE \"Name\" = 'Marketer';");
    }
}
