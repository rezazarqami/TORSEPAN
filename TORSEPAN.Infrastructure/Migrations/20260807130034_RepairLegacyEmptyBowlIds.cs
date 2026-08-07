using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RepairLegacyEmptyBowlIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE
                    empty_id uuid := '00000000-0000-0000-0000-000000000000';
                    replacement_id uuid := gen_random_uuid();
                    original_code varchar(50);
                BEGIN
                    SELECT "ProductionCode"
                    INTO original_code
                    FROM "Bowls"
                    WHERE "Id" = empty_id;

                    IF original_code IS NULL THEN
                        RETURN;
                    END IF;

                    INSERT INTO "Bowls" (
                        "Id", "ProductionCode", "BowlType", "HasNotes",
                        "InstrumentType", "Status", "Stage", "MaterialId")
                    SELECT
                        replacement_id,
                        '__repair__' || replace(replacement_id::text, '-', ''),
                        "BowlType", "HasNotes", "InstrumentType", "Status", "Stage", "MaterialId"
                    FROM "Bowls"
                    WHERE "Id" = empty_id;

                    UPDATE "ProductionEvents"
                    SET "BowlId" = replacement_id
                    WHERE "BowlId" = empty_id;

                    UPDATE "HandpanAssemblies"
                    SET "TopBowlId" = replacement_id
                    WHERE "TopBowlId" = empty_id;

                    UPDATE "HandpanAssemblies"
                    SET "BottomBowlId" = replacement_id
                    WHERE "BottomBowlId" = empty_id;

                    DELETE FROM "Bowls"
                    WHERE "Id" = empty_id;

                    UPDATE "Bowls"
                    SET "ProductionCode" = original_code
                    WHERE "Id" = replacement_id;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Repairing an invalid primary key is intentionally irreversible.
        }
    }
}
