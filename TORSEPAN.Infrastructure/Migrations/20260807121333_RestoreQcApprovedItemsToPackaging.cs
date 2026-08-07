using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestoreQcApprovedItemsToPackaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Handpans\" AS handpan SET \"Stage\" = 16, \"Status\" = 2 " +
                "WHERE handpan.\"Stage\" = 18 " +
                "AND EXISTS (SELECT 1 FROM \"ProductionEvents\" AS qc " +
                "WHERE qc.\"HandpanId\" = handpan.\"Id\" AND qc.\"Action\" = 8 AND qc.\"Result\" = 1) " +
                "AND NOT EXISTS (SELECT 1 FROM \"ProductionEvents\" AS packaging " +
                "WHERE packaging.\"HandpanId\" = handpan.\"Id\" AND packaging.\"Action\" = 9);");

            migrationBuilder.Sql(
                "UPDATE \"Bowls\" AS bowl SET \"Stage\" = 16, \"Status\" = 2 " +
                "FROM \"HandpanAssemblies\" AS assembly " +
                "INNER JOIN \"Handpans\" AS handpan ON handpan.\"AssemblyId\" = assembly.\"Id\" " +
                "WHERE handpan.\"Stage\" = 16 " +
                "AND (bowl.\"Id\" = assembly.\"TopBowlId\" OR bowl.\"Id\" = assembly.\"BottomBowlId\");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Production workflow history is intentionally not reverted.
        }
    }
}
