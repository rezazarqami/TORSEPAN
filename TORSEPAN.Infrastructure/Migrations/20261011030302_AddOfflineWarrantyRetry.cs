using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfflineWarrantyRetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WarrantyPending",
                table: "OfflineReceipts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
            migrationBuilder.CreateIndex(name: "IX_OfflineReceipts_WarrantyPending", table: "OfflineReceipts", column: "WarrantyPending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_OfflineReceipts_WarrantyPending", table: "OfflineReceipts");
            migrationBuilder.DropColumn(
                name: "WarrantyPending",
                table: "OfflineReceipts");
        }
    }
}
