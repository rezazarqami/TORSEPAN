using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20261015190000_UnifyScalesAndBowlNotes")]
public class UnifyScalesAndBowlNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Unknown historical counts stay null: never invent note counts for existing bowls.
        migrationBuilder.AddColumn<int>(name: "NoteCount", table: "Bowls", type: "integer", nullable: true);
        migrationBuilder.Sql("""
            CREATE TEMP TABLE unified_scale_map ON COMMIT DROP AS
            SELECT "Id" AS old_id,
                first_value("Id") OVER (
                    PARTITION BY lower(regexp_replace(translate(trim("Name"), 'يك', 'یک'), '\s+', ' ', 'g'))
                    ORDER BY "IsActive" DESC, "Id") AS canonical_id
            FROM "Scales";

            -- Remap every foreign key, including payroll and order references, before hiding aliases.
            DO $$
            DECLARE ref record;
            BEGIN
                FOR ref IN
                    SELECT ns.nspname AS schema_name, tbl.relname AS table_name, a.attname AS column_name
                    FROM pg_constraint c
                    JOIN pg_class tbl ON tbl.oid = c.conrelid
                    JOIN pg_namespace ns ON ns.oid = tbl.relnamespace
                    JOIN pg_attribute a ON a.attrelid = tbl.oid AND a.attnum = c.conkey[1]
                    WHERE c.contype = 'f' AND c.confrelid = '"Scales"'::regclass
                LOOP
                    EXECUTE format('UPDATE %I.%I AS target SET %I = map.canonical_id FROM unified_scale_map map WHERE target.%I = map.old_id AND map.old_id <> map.canonical_id',
                        ref.schema_name, ref.table_name, ref.column_name, ref.column_name);
                END LOOP;
            END $$;
            UPDATE "Scales" SET "IsActive" = false
            WHERE "Id" IN (SELECT old_id FROM unified_scale_map WHERE old_id <> canonical_id);
            UPDATE "Scales" SET "Usage" = 63;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Shared references cannot safely be split back into their former categories.
        throw new NotSupportedException("Restore the pre-migration database backup to revert unified scales.");
    }
}
