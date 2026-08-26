using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[Migration("20260826011500_AddDesignTypeRates")]
public partial class AddDesignTypeRates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "Rate",
            table: "DesignTypes",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.DropColumn(name: "Rate", table: "DesignTypes");
}
