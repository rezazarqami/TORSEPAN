using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderComposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "CustomerOrders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "CustomerOrders",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "CustomerOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ScaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScaleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DesignTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    DesignName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrderLines", x => x.Id);
                    table.CheckConstraint("CK_OrderLine_Quantity", "\"Quantity\" >= 1 AND \"Quantity\" <= 10000");
                    table.ForeignKey(
                        name: "FK_CustomerOrderLines_CustomerOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "CustomerOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerOrderLines_DesignTypes_DesignTypeId",
                        column: x => x.DesignTypeId,
                        principalTable: "DesignTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrderLines_Scales_ScaleId",
                        column: x => x.ScaleId,
                        principalTable: "Scales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderInstruments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TopBowlId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandpanId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderInstruments", x => x.Id);
                    table.CheckConstraint("CK_OrderInstrument_Slot", "\"Slot\" >= 1");
                    table.ForeignKey(
                        name: "FK_OrderInstruments_Bowls_TopBowlId",
                        column: x => x.TopBowlId,
                        principalTable: "Bowls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderInstruments_CustomerOrderLines_LineId",
                        column: x => x.LineId,
                        principalTable: "CustomerOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderInstruments_Handpans_HandpanId",
                        column: x => x.HandpanId,
                        principalTable: "Handpans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderInstruments_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderLines_DesignTypeId",
                table: "CustomerOrderLines",
                column: "DesignTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderLines_OrderId_Position",
                table: "CustomerOrderLines",
                columns: new[] { "OrderId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrderLines_ScaleId",
                table: "CustomerOrderLines",
                column: "ScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInstruments_AssignedByUserId",
                table: "OrderInstruments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderInstruments_Code",
                table: "OrderInstruments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderInstruments_HandpanId",
                table: "OrderInstruments",
                column: "HandpanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderInstruments_LineId_Slot",
                table: "OrderInstruments",
                columns: new[] { "LineId", "Slot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderInstruments_TopBowlId",
                table: "OrderInstruments",
                column: "TopBowlId",
                unique: true);
            // Deterministic IDs preserve all historical single-instrument orders and their existing links.
            migrationBuilder.Sql("""
                INSERT INTO "CustomerOrderLines" ("Id", "OrderId", "Position", "ScaleId", "ScaleName", "DesignTypeId", "DesignName", "Quantity")
                SELECT "Id", "Id", 1, "ScaleId", "ScaleName", NULL, 'دیزاین مشخص نشده', 1 FROM "CustomerOrders";
                INSERT INTO "OrderInstruments" ("Id", "LineId", "Slot", "Code", "TopBowlId", "HandpanId", "AssignedByUserId", "AssignedAtUtc")
                SELECT "Id", "Id", 1, upper("InstrumentCode"), "TopBowlId", "HandpanId", coalesce("CodeAssignedByUserId", "CreatedByUserId"), coalesce("CodeAssignedAtUtc", "CreatedAtUtc")
                FROM "CustomerOrders" WHERE "InstrumentCode" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not silently discard draft/multi-instrument data on an accidental downgrade.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM "CustomerOrders" WHERE "IsDraft")
                    OR EXISTS (SELECT 1 FROM "CustomerOrderLines" WHERE "Quantity" > 1 OR "DesignTypeId" IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM "CustomerOrderLines" GROUP BY "OrderId" HAVING count(*) > 1)
                  THEN RAISE EXCEPTION 'Order composition contains data unsupported by the previous version; restore a verified backup instead.';
                  END IF;
                END $$;
                """);

            migrationBuilder.DropTable(
                name: "OrderInstruments");

            migrationBuilder.DropTable(
                name: "CustomerOrderLines");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "CustomerOrders");
        }
    }
}
