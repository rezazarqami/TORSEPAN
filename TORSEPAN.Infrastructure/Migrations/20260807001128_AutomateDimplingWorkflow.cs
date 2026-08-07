using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AutomateDimplingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Bowls\" SET \"Stage\" = 2, \"Status\" = 2 WHERE \"Stage\" = 1;");

            migrationBuilder.AlterColumn<Guid>(
                name: "HandpanId",
                table: "ProductionEvents",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM \"ProductionEvents\" WHERE \"HandpanId\" IS NULL;");

            migrationBuilder.Sql(
                "UPDATE \"Bowls\" SET \"Stage\" = 1, \"Status\" = 1 WHERE \"Stage\" = 2;");

            migrationBuilder.AlterColumn<Guid>(
                name: "HandpanId",
                table: "ProductionEvents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
