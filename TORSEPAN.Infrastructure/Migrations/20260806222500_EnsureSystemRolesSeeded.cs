using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260806222500_EnsureSystemRolesSeeded")]
public partial class EnsureSystemRolesSeeded : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO "Roles" ("Id", "Name", "DisplayName") VALUES
                ('11111111-1111-1111-1111-111111111111', 'Administrator', 'مدیر سیستم'),
                ('22222222-2222-2222-2222-222222222222', 'ProductionManager', 'مدیر تولید'),
                ('33333333-3333-3333-3333-333333333333', 'Dimpler', 'دیمپل'),
                ('44444444-4444-4444-4444-444444444444', 'Shaper', 'شیپر'),
                ('55555555-5555-5555-5555-555555555555', 'Tuner', 'تیونر'),
                ('66666666-6666-6666-6666-666666666666', 'FineTuner', 'فاین تیونر'),
                ('77777777-7777-7777-7777-777777777777', 'QualityControl', 'کنترل کیفیت'),
                ('88888888-8888-8888-8888-888888888888', 'Workshop', 'سالن کار'),
                ('99999999-9999-9999-9999-999999999999', 'Warehouse', 'انبار')
            ON CONFLICT ("Name") DO UPDATE
            SET "DisplayName" = EXCLUDED."DisplayName";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
