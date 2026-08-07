using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHandpanScales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScaleId",
                table: "Handpans",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Scales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scales", x => x.Id);
                });

            migrationBuilder.Sql(
                "INSERT INTO \"Scales\" (\"Id\", \"Name\") VALUES " +
                "('90000000-0000-0000-0000-000000000001', '۹ نت'), " +
                "('90000000-0000-0000-0000-000000000002', 'سفارشی');");

            migrationBuilder.Sql(
                "UPDATE \"Handpans\" AS handpan SET \"ScaleId\" = " +
                "CASE WHEN top_bowl.\"InstrumentType\" = 2 " +
                "THEN '90000000-0000-0000-0000-000000000002'::uuid " +
                "ELSE '90000000-0000-0000-0000-000000000001'::uuid END " +
                "FROM \"HandpanAssemblies\" AS assembly " +
                "INNER JOIN \"Bowls\" AS top_bowl ON top_bowl.\"Id\" = assembly.\"TopBowlId\" " +
                "WHERE handpan.\"AssemblyId\" = assembly.\"Id\";");

            migrationBuilder.Sql(
                "UPDATE \"Bowls\" SET \"Stage\" = 10, \"Status\" = 2 " +
                "WHERE \"BowlType\" = 2 AND \"HasNotes\" = FALSE AND \"Stage\" = 8;");

            migrationBuilder.CreateIndex(
                name: "IX_Handpans_ScaleId",
                table: "Handpans",
                column: "ScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Scales_Name",
                table: "Scales",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Handpans_Scales_ScaleId",
                table: "Handpans",
                column: "ScaleId",
                principalTable: "Scales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Handpans_Scales_ScaleId",
                table: "Handpans");

            migrationBuilder.DropTable(
                name: "Scales");

            migrationBuilder.DropIndex(
                name: "IX_Handpans_ScaleId",
                table: "Handpans");

            migrationBuilder.DropColumn(
                name: "ScaleId",
                table: "Handpans");
        }
    }
}
