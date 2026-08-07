using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeBowlRoutingAndGlueRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Bowls\" SET \"Stage\" = 6, \"Status\" = 2 " +
                "WHERE \"BowlType\" = 2 AND \"HasNotes\" = FALSE AND \"Stage\" IN (1, 2);");

            migrationBuilder.Sql(
                "UPDATE \"Bowls\" SET \"Stage\" = 11, \"Status\" = 2 " +
                "WHERE \"Stage\" = 12;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Production routing data is intentionally not reverted.
        }
    }
}
