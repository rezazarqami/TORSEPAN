using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Materials",
                type: "integer",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Materials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "Materials",
                type: "integer",
                nullable: false,
                defaultValue: 3,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 4);

            migrationBuilder.Sql("""
                INSERT INTO "Materials" ("Id", "Name", "Category", "Quantity") VALUES
                ('11111111-1111-4111-8111-111111111111', 'کاسه رو', 1, 0),
                ('22222222-2222-4222-8222-222222222222', 'کاسه زیر', 2, 0),
                ('33333333-3333-4333-8333-333333333331', 'روغن', 3, 0),
                ('33333333-3333-4333-8333-333333333332', 'دستمال', 3, 0),
                ('33333333-3333-4333-8333-333333333333', 'سافت کیس', 3, 0),
                ('33333333-3333-4333-8333-333333333334', 'میدل کیس', 3, 0),
                ('33333333-3333-4333-8333-333333333335', 'هارد کیس', 3, 0),
                ('33333333-3333-4333-8333-333333333336', 'پایه', 3, 0)
                ON CONFLICT ("Name") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Materials");
        }
    }
}
