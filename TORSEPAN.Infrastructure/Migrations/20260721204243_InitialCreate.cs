using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bowls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BowlType = table.Column<int>(type: "integer", nullable: false),
                    HasNotes = table.Column<bool>(type: "boolean", nullable: false),
                    InstrumentType = table.Column<int>(type: "integer", nullable: false),
                    NoteCount = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bowls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HandpanAssemblies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TopBowlId = table.Column<Guid>(type: "uuid", nullable: false),
                    BottomBowlId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssemblyDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandpanAssemblies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandpanAssemblies_Bowls_BottomBowlId",
                        column: x => x.BottomBowlId,
                        principalTable: "Bowls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandpanAssemblies_Bowls_TopBowlId",
                        column: x => x.TopBowlId,
                        principalTable: "Bowls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Handpans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerialNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Handpans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Handpans_HandpanAssemblies_AssemblyId",
                        column: x => x.AssemblyId,
                        principalTable: "HandpanAssemblies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BowlId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssemblyId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandpanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionEvents_Bowls_BowlId",
                        column: x => x.BowlId,
                        principalTable: "Bowls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionEvents_HandpanAssemblies_AssemblyId",
                        column: x => x.AssemblyId,
                        principalTable: "HandpanAssemblies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionEvents_Handpans_HandpanId",
                        column: x => x.HandpanId,
                        principalTable: "Handpans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bowls_ProductionCode",
                table: "Bowls",
                column: "ProductionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandpanAssemblies_BottomBowlId",
                table: "HandpanAssemblies",
                column: "BottomBowlId");

            migrationBuilder.CreateIndex(
                name: "IX_HandpanAssemblies_TopBowlId",
                table: "HandpanAssemblies",
                column: "TopBowlId");

            migrationBuilder.CreateIndex(
                name: "IX_Handpans_AssemblyId",
                table: "Handpans",
                column: "AssemblyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Handpans_SerialNumber",
                table: "Handpans",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEvents_AssemblyId",
                table: "ProductionEvents",
                column: "AssemblyId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEvents_BowlId",
                table: "ProductionEvents",
                column: "BowlId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEvents_HandpanId",
                table: "ProductionEvents",
                column: "HandpanId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEvents_UserId",
                table: "ProductionEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionEvents");

            migrationBuilder.DropTable(
                name: "Handpans");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "HandpanAssemblies");

            migrationBuilder.DropTable(
                name: "Bowls");
        }
    }
}
