using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PromoteAssembliesToInstruments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO \"Handpans\" (\"Id\", \"AssemblyId\", \"SerialNumber\", \"Status\", \"Stage\", \"CreatedAt\", \"UpdatedAt\") " +
                "SELECT gen_random_uuid(), assembly.\"Id\", top_bowl.\"ProductionCode\", 2, " +
                "CASE WHEN top_bowl.\"Stage\" = 12 THEN 12 ELSE 11 END, " +
                "assembly.\"AssemblyDate\", NULL " +
                "FROM \"HandpanAssemblies\" AS assembly " +
                "INNER JOIN \"Bowls\" AS top_bowl ON top_bowl.\"Id\" = assembly.\"TopBowlId\" " +
                "WHERE NOT EXISTS (" +
                "SELECT 1 FROM \"Handpans\" AS handpan WHERE handpan.\"AssemblyId\" = assembly.\"Id\"" +
                ");");

            migrationBuilder.Sql(
                "UPDATE \"ProductionEvents\" AS production_event " +
                "SET \"HandpanId\" = handpan.\"Id\" " +
                "FROM \"Handpans\" AS handpan " +
                "WHERE production_event.\"AssemblyId\" = handpan.\"AssemblyId\" " +
                "AND production_event.\"HandpanId\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Historical production data is intentionally preserved.
        }
    }
}
