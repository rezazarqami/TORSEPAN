using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;
namespace TORSEPAN.Infrastructure.Migrations;
[DbContext(typeof(TORSEPANDbContext)), Migration("20261006110000_AddUserDeletion")]
public sealed class AddUserDeletion : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<bool>(name: "IsDeleted", table: "Users", type: "boolean", nullable: false, defaultValue: false);
    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "IsDeleted", table: "Users");
}
