using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

public partial class AddDesignTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DesignTypes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_DesignTypes", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_DesignTypes_Name", table: "DesignTypes", column: "Name", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "DesignTypes");
}
