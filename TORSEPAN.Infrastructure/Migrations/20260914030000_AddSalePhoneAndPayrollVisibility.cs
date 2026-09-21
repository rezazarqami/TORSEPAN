using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260914030000_AddSalePhoneAndPayrollVisibility")]
public partial class AddSalePhoneAndPayrollVisibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "BuyerPhoneNumber", table: "Handpans", type: "character varying(30)", maxLength: 30, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "ShowMyPayroll", table: "Users", type: "boolean", nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BuyerPhoneNumber", table: "Handpans");
        migrationBuilder.DropColumn(name: "ShowMyPayroll", table: "Users");
    }
}
