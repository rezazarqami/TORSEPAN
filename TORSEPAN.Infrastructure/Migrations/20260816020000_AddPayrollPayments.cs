using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[Migration("20260816020000_AddPayrollPayments")]
public partial class AddPayrollPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PayrollPayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                From = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                To = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PaidBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                HandpanIdsJson = table.Column<string>(type: "text", nullable: false),
                HandpanCodesJson = table.Column<string>(type: "text", nullable: false),
                LinesJson = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PayrollPayments", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_PayrollPayments_PaidAt", table: "PayrollPayments", column: "PaidAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("PayrollPayments");
}
