using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name", "DisplayName" },
                values: new object[,]
                {
                    {
                        Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        "Administrator",
                        "مدیر سیستم"
                    },
                    {
                        Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        "ProductionManager",
                        "مدیر تولید"
                    },
                    {
                        Guid.Parse("33333333-3333-3333-3333-333333333333"),
                        "Dimpler",
                        "دیمپل"
                    },
                    {
                        Guid.Parse("44444444-4444-4444-4444-444444444444"),
                        "Shaper",
                        "شیپر"
                    },
                    {
                        Guid.Parse("55555555-5555-5555-5555-555555555555"),
                        "Tuner",
                        "تیونر"
                    },
                    {
                        Guid.Parse("66666666-6666-6666-6666-666666666666"),
                        "FineTuner",
                        "فاین تیونر"
                    },
                    {
                        Guid.Parse("77777777-7777-7777-7777-777777777777"),
                        "QualityControl",
                        "کنترل کیفیت"
                    },
                    {
                        Guid.Parse("88888888-8888-8888-8888-888888888888"),
                        "Workshop",
                        "سالن کار"
                    },
                    {
                        Guid.Parse("99999999-9999-9999-9999-999999999999"),
                        "Warehouse",
                        "انبار"
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Guid.Parse("99999999-9999-9999-9999-999999999999")
                });

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}