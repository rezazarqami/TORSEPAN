using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;
public partial class AddMessagePush : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
            migrationBuilder.CreateTable(
                name: "MessagePushKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PublicKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrivateKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagePushKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessagePushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialVersion = table.Column<int>(type: "integer", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    EndpointHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    P256dh = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Auth = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagePushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagePushSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessagePushDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialVersion = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagePushDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessagePushDeliveries_MessagePushSubscriptions_Subscription~",
                        column: x => x.SubscriptionId,
                        principalTable: "MessagePushSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessagePushDeliveries_WorkshopMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "WorkshopMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessagePushDeliveries_MessageId_SubscriptionId",
                table: "MessagePushDeliveries",
                columns: new[] { "MessageId", "SubscriptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessagePushDeliveries_NextAttemptAt",
                table: "MessagePushDeliveries",
                column: "NextAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessagePushDeliveries_SubscriptionId",
                table: "MessagePushDeliveries",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MessagePushSubscriptions_EndpointHash",
                table: "MessagePushSubscriptions",
                column: "EndpointHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessagePushSubscriptions_UserId",
                table: "MessagePushSubscriptions",
                column: "UserId");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("MessagePushDeliveries");
        migrationBuilder.DropTable("MessagePushSubscriptions");
        migrationBuilder.DropTable("MessagePushKeys");
    }
}
