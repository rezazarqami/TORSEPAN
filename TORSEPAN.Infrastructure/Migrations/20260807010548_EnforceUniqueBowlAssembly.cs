using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueBowlAssembly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HandpanAssemblies_BottomBowlId",
                table: "HandpanAssemblies");

            migrationBuilder.DropIndex(
                name: "IX_HandpanAssemblies_TopBowlId",
                table: "HandpanAssemblies");

            migrationBuilder.CreateIndex(
                name: "IX_HandpanAssemblies_BottomBowlId",
                table: "HandpanAssemblies",
                column: "BottomBowlId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandpanAssemblies_TopBowlId",
                table: "HandpanAssemblies",
                column: "TopBowlId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HandpanAssemblies_BottomBowlId",
                table: "HandpanAssemblies");

            migrationBuilder.DropIndex(
                name: "IX_HandpanAssemblies_TopBowlId",
                table: "HandpanAssemblies");

            migrationBuilder.CreateIndex(
                name: "IX_HandpanAssemblies_BottomBowlId",
                table: "HandpanAssemblies",
                column: "BottomBowlId");

            migrationBuilder.CreateIndex(
                name: "IX_HandpanAssemblies_TopBowlId",
                table: "HandpanAssemblies",
                column: "TopBowlId");
        }
    }
}
