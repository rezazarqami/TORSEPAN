using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260814050000_AddUserTitleAndRepairBakeEvents")]
public partial class AddUserTitleAndRepairBakeEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "Title", table: "Users", type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "");
        migrationBuilder.Sql("UPDATE \"ProductionEvents\" SET \"Action\" = 4 WHERE \"Description\" ILIKE '%Bake completed%' AND \"Action\" <> 4;");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name: "Title", table: "Users");
}
