using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "CustomerOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScaleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DurationDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstrumentCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TopBowlId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandpanId = table.Column<Guid>(type: "uuid", nullable: true),
                    CodeAssignedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CodeAssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerOrders_Bowls_TopBowlId",
                        column: x => x.TopBowlId,
                        principalTable: "Bowls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerOrders_Handpans_HandpanId",
                        column: x => x.HandpanId,
                        principalTable: "Handpans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerOrders_Scales_ScaleId",
                        column: x => x.ScaleId,
                        principalTable: "Scales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrders_Users_CodeAssignedByUserId",
                        column: x => x.CodeAssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerOrders_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderReminders",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Milestone = table.Column<int>(type: "integer", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderReminders", x => new { x.OrderId, x.Milestone });
                    table.ForeignKey(
                        name: "FK_OrderReminders_CustomerOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "CustomerOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_CodeAssignedByUserId",
                table: "CustomerOrders",
                column: "CodeAssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_CreatedAtUtc",
                table: "CustomerOrders",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_CreatedByUserId",
                table: "CustomerOrders",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_DueAtUtc",
                table: "CustomerOrders",
                column: "DueAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_HandpanId",
                table: "CustomerOrders",
                column: "HandpanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_InstrumentCode",
                table: "CustomerOrders",
                column: "InstrumentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_ScaleId",
                table: "CustomerOrders",
                column: "ScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_TopBowlId",
                table: "CustomerOrders",
                column: "TopBowlId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderReminders_SentAtUtc_NextAttemptAtUtc",
                table: "OrderReminders",
                columns: new[] { "SentAtUtc", "NextAttemptAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "OrderReminders");

            migrationBuilder.DropTable(
                name: "CustomerOrders");

        }
    }
}
