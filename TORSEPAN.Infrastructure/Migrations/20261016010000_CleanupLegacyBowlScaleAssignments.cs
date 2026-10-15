using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20261016010000_CleanupLegacyBowlScaleAssignments")]
public sealed class CleanupLegacyBowlScaleAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            -- Run once through EF migration history, before this API accepts requests.
            -- Keep a per-bowl audit of old values; never delete scale catalog/history rows.
            LOCK TABLE "Bowls" IN SHARE ROW EXCLUSIVE MODE;
            CREATE TABLE IF NOT EXISTS "BowlScaleCleanup20261016" (
                "BowlId" uuid PRIMARY KEY,
                "ProductionCode" text NOT NULL,
                "Stage" integer NOT NULL,
                "OldScaleId" uuid NULL,
                "OldScaleName" text NULL,
                "OldNoteCount" integer NULL,
                "OldIsCustomScale" boolean NOT NULL,
                "OldOfflineVersion" uuid NOT NULL,
                "NewNoteCount" integer NULL,
                "Reason" text NOT NULL,
                "AppliedAtUtc" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TEMP TABLE bowl_scale_cleanup_targets ON COMMIT DROP AS
            WITH legacy_scales AS (
                SELECT "Id",
                    lower(regexp_replace(
                        translate(trim("Name"), '۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩', '01234567890123456789'),
                        '[\s()_+،,/:–—−-]', '', 'g')) AS normalized
                FROM "Scales"
            ), counts AS (
                SELECT "Id", CASE
                    WHEN normalized ~ '^(2|دو)(نت|نوت)زیرfو?g$' THEN 2
                    WHEN normalized ~ '^(3|سه)(نت|نوت)زیرeو?fو?g$' THEN 3
                    ELSE NULL END AS note_count
                FROM legacy_scales
            )
            SELECT b."Id", b."NoteCount" AS note_count, 'waiting-for-shape'::text AS reason
            FROM "Bowls" b
            WHERE b."Stage" = 4 AND b."Status" <> 5
                AND (b."ScaleId" IS NOT NULL OR b."IsCustomScale")
                AND NOT EXISTS (SELECT 1 FROM "HandpanAssemblies" a
                    WHERE a."TopBowlId" = b."Id" OR a."BottomBowlId" = b."Id")
            UNION ALL
            SELECT b."Id", c.note_count, 'legacy-bottom-note-count'::text
            FROM "Bowls" b JOIN counts c ON c."Id" = b."ScaleId"
            WHERE b."BowlType" = 2 AND b."Stage" IN (6, 8, 10)
                AND b."Status" <> 5 AND c.note_count IS NOT NULL
                AND NOT EXISTS (SELECT 1 FROM "HandpanAssemblies" a
                    WHERE a."TopBowlId" = b."Id" OR a."BottomBowlId" = b."Id");

            INSERT INTO "BowlScaleCleanup20261016" (
                "BowlId", "ProductionCode", "Stage", "OldScaleId", "OldScaleName",
                "OldNoteCount", "OldIsCustomScale", "OldOfflineVersion", "NewNoteCount", "Reason")
            SELECT b."Id", b."ProductionCode", b."Stage", b."ScaleId", s."Name",
                b."NoteCount", b."IsCustomScale", b."OfflineVersion", t.note_count, t.reason
            FROM "Bowls" b JOIN bowl_scale_cleanup_targets t ON t."Id" = b."Id"
            LEFT JOIN "Scales" s ON s."Id" = b."ScaleId"
            ON CONFLICT ("BowlId") DO NOTHING;

            UPDATE "Bowls" b
            SET "ScaleId" = NULL, "IsCustomScale" = false,
                "NoteCount" = t.note_count, "OfflineVersion" = gen_random_uuid()
            FROM bowl_scale_cleanup_targets t WHERE t."Id" = b."Id";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // New shaping may have happened since cleanup; never overwrite those user changes.
        throw new NotSupportedException("Use the per-bowl cleanup audit to review recovery without overwriting later work.");
    }
}
