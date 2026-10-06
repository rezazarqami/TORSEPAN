using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Migrations;
[DbContext(typeof(TORSEPANDbContext)), Migration("20261005193000_AddPersonalProfile")]
public sealed class AddPersonalProfile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "CredentialVersion", table: "RefreshTokens", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<int>(name: "CredentialVersion", table: "Users", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<byte[]>(name: "AvatarPng", table: "Users", type: "bytea", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "AvatarVersion", table: "Users", type: "uuid", nullable: true);
        // Match the existing case-insensitive login lookup, including concurrent account edits.
        migrationBuilder.Sql("CREATE UNIQUE INDEX \"IX_Users_NormalizedUserName\" ON \"Users\" (upper(btrim(\"UserName\")));");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX \"IX_Users_NormalizedUserName\";");
        migrationBuilder.DropColumn(name: "CredentialVersion", table: "Users");
        migrationBuilder.DropColumn(name: "CredentialVersion", table: "RefreshTokens");
        migrationBuilder.DropColumn(name: "AvatarPng", table: "Users");
        migrationBuilder.DropColumn(name: "AvatarVersion", table: "Users");
    }
}
