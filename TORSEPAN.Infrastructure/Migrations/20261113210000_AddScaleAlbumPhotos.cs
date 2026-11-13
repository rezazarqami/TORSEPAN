using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20261113210000_AddScaleAlbumPhotos")]
public sealed class AddScaleAlbumPhotos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ScaleAlbumPhotos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ScaleId = table.Column<Guid>(type: "uuid", nullable: false),
                Image = table.Column<byte[]>(type: "bytea", nullable: false),
                NotesJson = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ScaleAlbumPhotos", x => x.Id);
                table.ForeignKey("FK_ScaleAlbumPhotos_Scales_ScaleId", x => x.ScaleId,
                    "Scales", "Id", onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex("IX_ScaleAlbumPhotos_ScaleId_CreatedAt", "ScaleAlbumPhotos", new[] { "ScaleId", "CreatedAt" });
        migrationBuilder.Sql("""
            INSERT INTO "ScaleAlbumPhotos" ("Id", "ScaleId", "Image", "NotesJson", "CreatedAt")
            SELECT md5(p."ScaleId"::text || shell.name)::uuid, p."ScaleId", shell.image,
                   (SELECT jsonb_agg(jsonb_build_object(
                       'Pitch', coalesce(note->>'Pitch', note->>'pitch'),
                       'X', coalesce(note->'X', note->'x'),
                       'Y', coalesce(note->'Y', note->'y')) ORDER BY position)::text
                    FROM jsonb_array_elements(coalesce(p."PlaygroundJson"::jsonb->'Notes',
                        p."PlaygroundJson"::jsonb->'notes', '[]'::jsonb)) WITH ORDINALITY AS n(note, position)
                    WHERE coalesce((note->>'Bottom')::boolean, (note->>'bottom')::boolean, false) = shell.is_bottom),
                   TIMESTAMPTZ '2026-01-01 00:00:00+00' + shell.sort * INTERVAL '1 second'
            FROM "ScalePhotos" p
            CROSS JOIN LATERAL (VALUES ('top', p."Top", false, 0), ('bottom', p."Bottom", true, 1))
                AS shell(name, image, is_bottom, sort)
            WHERE shell.image IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("ScaleAlbumPhotos");
}
