using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(TORSEPANDbContext))]
    [Migration("20261016094948_SeparateScaleFamiliesAndNotes")]
    public partial class SeparateScaleFamiliesAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CanonicalScaleId",
                table: "Scales",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LegacyNoteCount",
                table: "Scales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoteCount",
                table: "Handpans",
                type: "integer",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "LegacyScaleId", table: "Bowls", type: "uuid", nullable: true);

            migrationBuilder.Sql("""
                LOCK TABLE "Bowls", "Handpans", "Scales", "CustomerOrders", "CustomerOrderLines" IN SHARE ROW EXCLUSIVE MODE;
                CREATE TABLE "ScaleFamilyNormalizationAudit" (
                    "EntityType" text NOT NULL, "EntityId" uuid NOT NULL, "Before" jsonb NOT NULL,
                    "AppliedAtUtc" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    PRIMARY KEY ("EntityType", "EntityId")
                );
                CREATE FUNCTION pg_temp.family_canonical(value text) RETURNS text LANGUAGE sql IMMUTABLE AS $$
                 SELECT CASE lower(regexp_replace(trim(value), '\s+', '', 'g'))
                  WHEN 'dkurd' THEN 'D Kurd' WHEN 'دیکورد' THEN 'D Kurd' WHEN 'دیکرد' THEN 'D Kurd'
                  WHEN 'cyclops' THEN 'Cyclops' WHEN 'سایکلوپس' THEN 'Cyclops' WHEN 'سیکلوپس' THEN 'Cyclops'
                  WHEN 'eamara' THEN 'E Amara' WHEN 'میآمارا' THEN 'E Amara' WHEN 'میهآمارا' THEN 'E Amara'
                  ELSE regexp_replace(trim(value), '\s+', ' ', 'g') END;
                $$;
                CREATE FUNCTION pg_temp.parse_scale_family(value text)
                RETURNS TABLE (family text, note_count integer, is_bottom boolean) LANGUAGE plpgsql AS $$
                DECLARE n text; m text[]; candidate text; amount integer; explicit_count boolean;
                BEGIN
                 n := regexp_replace(trim(translate(value,'۰۱۲۳۴۵۶۷۸۹٠١٢٣٤٥٦٧٨٩','01234567890123456789')), '\s+', ' ', 'g');
                 is_bottom := n ~ '(^زیر\s|\sزیر$)';
                 n := trim(regexp_replace(n,'^(روی|رو|زیر)\s+|\s+(رو|زیر)$','','g'));
                 IF n ~ '^[0-9]+\s*(نت|نوت)\s+(رو|زیر)(\s|$)' THEN family:=NULL;note_count:=NULL;RETURN NEXT;RETURN;END IF;
                 m := regexp_match(n,'^(d\s*kurd|دی\s*کورد|دی\s*کرد|دیکورد|دیکرد)\s*([0-9]{1,2})?(\s+(cyclops|سایکلوپس))?$','i');
                 IF m IS NOT NULL THEN
                  family := 'D Kurd' || CASE WHEN m[4] IS NOT NULL THEN ' Cyclops' ELSE '' END;
                  note_count := CASE WHEN m[2] IS NOT NULL AND m[2]::int BETWEEN 1 AND 60 THEN m[2]::int ELSE NULL END;
                  IF m[2] IS NOT NULL AND NOT (m[2]::int BETWEEN 1 AND 60) THEN family := n; END IF;
                  RETURN NEXT; RETURN;
                 END IF;
                 m := regexp_match(n,'^([0-9]{1,2})\s*(نت|نوت|notes?)\s+(.+)$','i');
                 explicit_count := m IS NOT NULL;
                 IF explicit_count THEN amount := m[1]::int; candidate := m[3];
                 ELSE
                  m := regexp_match(n,'^(.+?)\s+([0-9]{1,2})(\s*(نت|نوت|notes?))?$','i');
                  IF m IS NOT NULL THEN candidate := m[1]; amount := m[2]::int; END IF;
                 END IF;
                 IF candidate IS NOT NULL AND amount BETWEEN 1 AND 60 AND (explicit_count OR amount >= 5)
                   AND candidate ~ '[A-Za-zآ-ی]' AND trim(candidate) NOT IN ('رو','روی','زیر') AND candidate !~ '^[0-9]' THEN
                  family := pg_temp.family_canonical(candidate); note_count := amount;
                 ELSIF n ~ '^[0-9]+\s*(نت|نوت)(\s*\([^)]*\))?$' OR n ~ '^[0-9]' OR n IN ('رو','روی','زیر','') THEN
                  family := NULL; note_count := NULL;
                 ELSE family := pg_temp.family_canonical(n); note_count := NULL;
                 END IF;
                 RETURN NEXT;
                END $$;
                CREATE TEMP TABLE family_source ON COMMIT DROP AS
                 SELECT s."Id", s."Name", s."IsActive", p.family, p.note_count, p.is_bottom, s."Name" ~* '(نت|نوت|notes?|d\s*kurd|دی\s*کورد|دی\s*کرد|دیکورد|دیکرد)' AS infer_bowl_count,
                    lower(regexp_replace(p.family,'\s+','','g')) AS family_key
                 FROM "Scales" s CROSS JOIN LATERAL pg_temp.parse_scale_family(s."Name") p
                 WHERE s."CanonicalScaleId" IS NULL AND p.family IS NOT NULL;
                CREATE TEMP TABLE family_groups ON COMMIT DROP AS
                 SELECT family_key, min(family) AS family_name FROM family_source GROUP BY family_key;
                INSERT INTO "Scales" ("Id","Name","IsActive","Usage")
                 SELECT gen_random_uuid(),g.family_name,EXISTS (SELECT 1 FROM family_source active_source WHERE active_source.family_key=g.family_key AND active_source."IsActive"),63 FROM family_groups g
                 WHERE NOT EXISTS (SELECT 1 FROM family_source s WHERE s.family_key=g.family_key AND s.note_count IS NULL AND NOT s.is_bottom)
                 ON CONFLICT ("Name") DO NOTHING;
                CREATE TEMP TABLE family_map ON COMMIT DROP AS
                 SELECT source."Id" AS old_id, source.note_count, source.is_bottom, source.infer_bowl_count, target."Id" AS canonical_id
                 FROM family_source source CROSS JOIN LATERAL (
                  SELECT s."Id" FROM "Scales" s CROSS JOIN LATERAL pg_temp.parse_scale_family(s."Name") p
                  WHERE s."CanonicalScaleId" IS NULL AND p.family IS NOT NULL AND p.note_count IS NULL AND NOT p.is_bottom
                    AND lower(regexp_replace(p.family,'\s+','','g'))=source.family_key
                  ORDER BY CASE WHEN s."Name"=(SELECT g.family_name FROM family_groups g WHERE g.family_key=source.family_key) THEN 0 ELSE 1 END,s."IsActive" DESC,s."Id" LIMIT 1
                 ) target WHERE source."Id"<>target."Id";

                INSERT INTO "ScaleFamilyNormalizationAudit" ("EntityType","EntityId","Before")
                 SELECT 'Scales',s."Id",to_jsonb(s) FROM "Scales" s WHERE NOT s."IsActive"
                 AND EXISTS (SELECT 1 FROM family_map m JOIN family_source src ON src."Id"=m.old_id WHERE m.canonical_id=s."Id" AND src."IsActive");
                UPDATE "Scales" s SET "IsActive"=true WHERE NOT s."IsActive"
                 AND EXISTS (SELECT 1 FROM family_map m JOIN family_source src ON src."Id"=m.old_id WHERE m.canonical_id=s."Id" AND src."IsActive");

                INSERT INTO "ScaleFamilyNormalizationAudit" ("EntityType","EntityId","Before")
                 SELECT 'Bowls',b."Id",to_jsonb(b) FROM "Bowls" b
                 WHERE b."ScaleId" IN (SELECT old_id FROM family_map)
                   OR (b."BowlType"=2 AND NOT b."HasNotes" AND b."NoteCount" IS NULL);
                UPDATE "Bowls" b SET "LegacyScaleId"=coalesce(b."LegacyScaleId",b."ScaleId"),"ScaleId"=m.canonical_id,
                 "NoteCount"=CASE WHEN b."NoteCount" IS NOT NULL THEN b."NoteCount"
                  WHEN b."BowlType"=2 AND NOT b."HasNotes" THEN 0
                  WHEN b."BowlType"=1 AND NOT m.is_bottom AND m.infer_bowl_count AND NOT EXISTS (
                    SELECT 1 FROM "HandpanAssemblies" a WHERE a."TopBowlId"=b."Id" OR a."BottomBowlId"=b."Id") THEN m.note_count
                  ELSE NULL END,
                 "OfflineVersion"=gen_random_uuid()
                 FROM family_map m WHERE b."ScaleId"=m.old_id;
                UPDATE "Bowls" SET "NoteCount"=0,"OfflineVersion"=gen_random_uuid()
                 WHERE "BowlType"=2 AND NOT "HasNotes" AND "NoteCount" IS NULL;

                CREATE TEMP TABLE handpan_family_targets ON COMMIT DROP AS
                 SELECT h."Id",coalesce(m.canonical_id,h."ScaleId") AS scale_id,
                  coalesce(h."NoteCount",m.note_count,
                    CASE WHEN t."NoteCount" IS NOT NULL AND (b."NoteCount" IS NOT NULL OR NOT b."HasNotes")
                      AND t."NoteCount"::bigint+CASE WHEN NOT b."HasNotes" THEN 0 ELSE b."NoteCount" END <= 2147483647
                    THEN t."NoteCount"+CASE WHEN NOT b."HasNotes" THEN 0 ELSE b."NoteCount" END ELSE NULL END) AS note_count
                 FROM "Handpans" h LEFT JOIN family_map m ON m.old_id=h."ScaleId"
                 JOIN "HandpanAssemblies" a ON a."Id"=h."AssemblyId"
                 JOIN "Bowls" t ON t."Id"=a."TopBowlId" JOIN "Bowls" b ON b."Id"=a."BottomBowlId"
                 WHERE m.old_id IS NOT NULL OR (h."NoteCount" IS NULL AND t."NoteCount" IS NOT NULL AND (b."NoteCount" IS NOT NULL OR NOT b."HasNotes"));
                INSERT INTO "ScaleFamilyNormalizationAudit" ("EntityType","EntityId","Before")
                 SELECT 'Handpans',h."Id",to_jsonb(h) FROM "Handpans" h JOIN handpan_family_targets t ON t."Id"=h."Id";
                UPDATE "Handpans" h SET "ScaleId"=t.scale_id,"NoteCount"=t.note_count,"OfflineVersion"=gen_random_uuid()
                 FROM handpan_family_targets t WHERE t."Id"=h."Id";

                INSERT INTO "ScaleFamilyNormalizationAudit" ("EntityType","EntityId","Before")
                 SELECT 'CustomerOrders',o."Id",to_jsonb(o) FROM "CustomerOrders" o JOIN family_map m ON m.old_id=o."ScaleId";
                UPDATE "CustomerOrders" o SET "ScaleId"=m.canonical_id FROM family_map m WHERE o."ScaleId"=m.old_id;
                INSERT INTO "ScaleFamilyNormalizationAudit" ("EntityType","EntityId","Before")
                 SELECT 'CustomerOrderLines',o."Id",to_jsonb(o) FROM "CustomerOrderLines" o JOIN family_map m ON m.old_id=o."ScaleId";
                UPDATE "CustomerOrderLines" o SET "ScaleId"=m.canonical_id FROM family_map m WHERE o."ScaleId"=m.old_id;

                -- Keep payroll rates on their original, numbered aliases; rate matching uses this metadata and the bowl/instrument count.
                INSERT INTO "ScaleFamilyNormalizationAudit" ("EntityType","EntityId","Before")
                 SELECT 'Scales',s."Id",to_jsonb(s) FROM "Scales" s JOIN family_map m ON m.old_id=s."Id";
                UPDATE "Scales" s SET "CanonicalScaleId"=m.canonical_id,"LegacyNoteCount"=m.note_count,"IsActive"=false
                 FROM family_map m WHERE s."Id"=m.old_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Review ScaleFamilyNormalizationAudit before recovering old scale identities or note counts; preserve later production changes.");
        }
    }
}
