using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20261016020000_NormalizeRemainingLegacyBowlScales")]
public sealed class NormalizeRemainingLegacyBowlScales : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            LOCK TABLE "Bowls", "Scales" IN SHARE ROW EXCLUSIVE MODE;
            CREATE TABLE IF NOT EXISTS "BowlScaleCleanup20261016Extended" (
                "BowlId" uuid PRIMARY KEY, "ProductionCode" text NOT NULL, "Stage" integer NOT NULL,
                "OldScaleId" uuid NULL, "OldScaleName" text NULL, "OldNoteCount" integer NULL,
                "OldIsCustomScale" boolean NOT NULL, "OldOfflineVersion" uuid NOT NULL,
                "NewScaleId" uuid NULL, "NewNoteCount" integer NULL, "Reason" text NOT NULL,
                "AppliedAtUtc" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TEMP TABLE remaining_legacy_scale_names ON COMMIT DROP AS
            SELECT "Id", "Name", "IsActive",
                lower(regexp_replace(translate(trim("Name"),
                    '۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', '01234567890123456789'), '\s+', '', 'g')) AS normalized
            FROM "Scales";
            CREATE TEMP TABLE named_bottom_scale_map (scale_key text, display_name text) ON COMMIT DROP;
            INSERT INTO named_bottom_scale_map VALUES
                ('daegean21', 'D aegean 21'), ('dhijaz14', 'D hijaz 14'),
                ('dkurd19', 'Dkurd 19'), ('eamara', 'E Amara'), ('f#pygmy', 'F# pygmy');

            -- Create a canonical scale only if a scoped bottom bowl actually needs it.
            -- Never rename/delete the old catalog entry or change its other references.
            INSERT INTO "Scales" ("Id", "Name", "IsActive", "Usage")
            SELECT gen_random_uuid(), m.display_name, true, 63
            FROM named_bottom_scale_map m
            WHERE NOT EXISTS (SELECT 1 FROM remaining_legacy_scale_names s WHERE s.normalized = m.scale_key)
                AND EXISTS (
                    SELECT 1 FROM "Bowls" b JOIN remaining_legacy_scale_names s ON s."Id" = b."ScaleId"
                    WHERE b."BowlType" = 2 AND b."Stage" IN (6, 8, 10) AND b."Status" <> 5
                        AND s.normalized IN ('زیر' || m.scale_key, m.scale_key || 'زیر')
                        AND NOT EXISTS (SELECT 1 FROM "HandpanAssemblies" a
                            WHERE a."TopBowlId" = b."Id" OR a."BottomBowlId" = b."Id")
                )
            ON CONFLICT ("Name") DO NOTHING;

            CREATE TEMP TABLE remaining_bowl_scale_targets ON COMMIT DROP AS
            WITH note_counts AS (
                SELECT "Id", CASE
                    WHEN normalized ~ '^(4|چهار)(نت|نوت)زیر$' THEN 4
                    WHEN normalized ~ '^(5|پنج)(نت|نوت)زیر$' THEN 5
                    WHEN normalized ~ '^(6|شش|شیش)(نت|نوت)زیر$' THEN 6
                    WHEN normalized ~ '^(7|هفت)(نت|نوت)زیر$' THEN 7
                    WHEN normalized ~ '^(9|نه)(نت|نوت)زیر$' THEN 9
                    ELSE NULL END AS note_count
                FROM remaining_legacy_scale_names
            ), canonical_named_scales AS (
                SELECT m.scale_key, (
                    SELECT s."Id" FROM "Scales" s
                    WHERE lower(regexp_replace(translate(trim(s."Name"),
                        '۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', '01234567890123456789'), '\s+', '', 'g')) = m.scale_key
                    ORDER BY s."IsActive" DESC, s."Id" LIMIT 1
                ) AS canonical_id
                FROM named_bottom_scale_map m
            )
            SELECT b."Id", NULL::uuid AS new_scale_id, n.note_count, 'bottom-note-count'::text AS reason
            FROM "Bowls" b JOIN note_counts n ON n."Id" = b."ScaleId"
            WHERE b."BowlType" = 2 AND b."Stage" IN (6, 8, 10) AND b."Status" <> 5
                AND n.note_count IS NOT NULL
                AND NOT EXISTS (SELECT 1 FROM "HandpanAssemblies" a
                    WHERE a."TopBowlId" = b."Id" OR a."BottomBowlId" = b."Id")
            UNION ALL
            SELECT b."Id", c.canonical_id, NULL::integer, 'bottom-named-scale'::text
            FROM "Bowls" b JOIN remaining_legacy_scale_names s ON s."Id" = b."ScaleId"
            JOIN canonical_named_scales c ON s.normalized IN ('زیر' || c.scale_key, c.scale_key || 'زیر')
            WHERE b."BowlType" = 2 AND b."Stage" IN (6, 8, 10) AND b."Status" <> 5
                AND c.canonical_id IS NOT NULL
                AND NOT EXISTS (SELECT 1 FROM "HandpanAssemblies" a
                    WHERE a."TopBowlId" = b."Id" OR a."BottomBowlId" = b."Id");

            INSERT INTO "BowlScaleCleanup20261016Extended" (
                "BowlId", "ProductionCode", "Stage", "OldScaleId", "OldScaleName", "OldNoteCount",
                "OldIsCustomScale", "OldOfflineVersion", "NewScaleId", "NewNoteCount", "Reason")
            SELECT b."Id", b."ProductionCode", b."Stage", b."ScaleId", s."Name", b."NoteCount",
                b."IsCustomScale", b."OfflineVersion", t.new_scale_id, t.note_count, t.reason
            FROM "Bowls" b JOIN remaining_bowl_scale_targets t ON t."Id" = b."Id"
            LEFT JOIN "Scales" s ON s."Id" = b."ScaleId"
            ON CONFLICT ("BowlId") DO NOTHING;

            UPDATE "Bowls" b SET "ScaleId" = t.new_scale_id, "NoteCount" = t.note_count,
                "IsCustomScale" = false, "OfflineVersion" = gen_random_uuid()
            FROM remaining_bowl_scale_targets t WHERE t."Id" = b."Id";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Review the per-bowl cleanup audit before recovering values, preserving later user edits.");
    }
}
