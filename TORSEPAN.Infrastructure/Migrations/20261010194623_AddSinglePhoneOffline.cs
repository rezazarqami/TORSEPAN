using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSinglePhoneOffline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing manually-authored migrations already added photos/accounting; only offline DDL belongs here.
            migrationBuilder.AddColumn<Guid>(
                name: "OfflineVersion",
                table: "Handpans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OfflineVersion",
                table: "HandpanAssemblies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OfflineVersion",
                table: "Bowls",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "OfflineDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrolledUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineDevices", x => x.Id);
                    table.CheckConstraint("CK_OfflineDevices_Singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "OfflineReceipts",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrarId = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadJson = table.Column<string>(type: "character varying(16384)", maxLength: 16384, nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VersionsJson = table.Column<string>(type: "text", nullable: false),
                    OccurredUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfflineReceipts", x => x.OperationId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OfflineReceipts_DeviceId_AcceptedUtc",
                table: "OfflineReceipts",
                columns: new[] { "DeviceId", "AcceptedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfflineDevices");

            migrationBuilder.DropTable(
                name: "OfflineReceipts");

            migrationBuilder.DropColumn(
                name: "OfflineVersion",
                table: "Handpans");

            migrationBuilder.DropColumn(
                name: "OfflineVersion",
                table: "HandpanAssemblies");

            migrationBuilder.DropColumn(
                name: "OfflineVersion",
                table: "Bowls");

        }
    }
}
