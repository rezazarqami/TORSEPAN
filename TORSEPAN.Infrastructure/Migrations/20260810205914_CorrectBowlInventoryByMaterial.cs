using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorrectBowlInventoryByMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BottomBowlQuantity",
                table: "Materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                DELETE FROM "Materials"
                WHERE "Category" IN (1, 2)
                  AND "Name" IN ('کاسه رو', 'کاسه زیر');
                """);

            migrationBuilder.AddColumn<int>(
                name: "TopBowlQuantity",
                table: "Materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BottomBowlQuantity",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "TopBowlQuantity",
                table: "Materials");
        }
    }
}
