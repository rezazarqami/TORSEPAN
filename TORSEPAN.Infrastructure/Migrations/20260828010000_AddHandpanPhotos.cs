using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[Migration("20260828010000_AddHandpanPhotos")]
public partial class AddHandpanPhotos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HandpanPhotos",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                HandpanId = table.Column<Guid>(type: "uuid", nullable: false),
                Image = table.Column<byte[]>(type: "bytea", nullable: false),
                Thumbnail = table.Column<byte[]>(type: "bytea", nullable: false),
                ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HandpanPhotos", x => x.Id);
                table.ForeignKey("FK_HandpanPhotos_Handpans_HandpanId", x => x.HandpanId, "Handpans", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_HandpanPhotos_Users_UploadedByUserId", x => x.UploadedByUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_HandpanPhotos_HandpanId_CreatedAt", "HandpanPhotos", new[] { "HandpanId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_HandpanPhotos_UploadedByUserId", "HandpanPhotos", "UploadedByUserId");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("HandpanPhotos");
}
