using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeBowlEnumValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Bowls\" SET \"BowlType\" = \"BowlType\" + 1, " +
                "\"InstrumentType\" = \"InstrumentType\" + 1 " +
                "WHERE \"BowlType\" IN (0, 1) AND \"InstrumentType\" IN (0, 1);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"Bowls\" SET \"BowlType\" = \"BowlType\" - 1, " +
                "\"InstrumentType\" = \"InstrumentType\" - 1 " +
                "WHERE \"BowlType\" IN (1, 2) AND \"InstrumentType\" IN (1, 2);");
        }
    }
}
