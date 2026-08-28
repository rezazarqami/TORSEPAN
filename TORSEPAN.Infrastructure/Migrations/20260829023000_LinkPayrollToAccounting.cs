using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext)), Migration("20260829023000_LinkPayrollToAccounting")]
public partial class LinkPayrollToAccounting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "PayrollPaymentId", table: "AccountingDocuments", type: "uuid", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_AccountingDocuments_PayrollPaymentId", table: "AccountingDocuments", column: "PayrollPaymentId", unique: true);
        migrationBuilder.AddForeignKey(name: "FK_AccountingDocuments_PayrollPayments_PayrollPaymentId", table: "AccountingDocuments", column: "PayrollPaymentId", principalTable: "PayrollPayments", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_AccountingDocuments_PayrollPayments_PayrollPaymentId", table: "AccountingDocuments");
        migrationBuilder.DropIndex(name: "IX_AccountingDocuments_PayrollPaymentId", table: "AccountingDocuments");
        migrationBuilder.DropColumn(name: "PayrollPaymentId", table: "AccountingDocuments");
    }
}
