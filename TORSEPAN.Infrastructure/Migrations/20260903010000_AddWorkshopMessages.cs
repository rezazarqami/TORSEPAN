using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;
#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;
[DbContext(typeof(TORSEPANDbContext)),Migration("20260903010000_AddWorkshopMessages")]
public sealed class AddWorkshopMessages : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.CreateTable("WorkshopMessages",columns:t=>new{
            Id=t.Column<Guid>(type:"uuid",nullable:false),SenderId=t.Column<Guid>(type:"uuid",nullable:false),
            Title=t.Column<string>(type:"character varying(150)",maxLength:150,nullable:false),
            Body=t.Column<string>(type:"character varying(4000)",maxLength:4000,nullable:false),
            Broadcast=t.Column<bool>(type:"boolean",nullable:false),CreatedAt=t.Column<DateTime>(type:"timestamp with time zone",nullable:false)
        },constraints:t=>{t.PrimaryKey("PK_WorkshopMessages",x=>x.Id);t.ForeignKey("FK_WorkshopMessages_Users_SenderId",x=>x.SenderId,"Users","Id",onDelete:ReferentialAction.Restrict);});
        m.CreateTable("WorkshopMessageReceipts",columns:t=>new{
            MessageId=t.Column<Guid>(type:"uuid",nullable:false),RecipientId=t.Column<Guid>(type:"uuid",nullable:false),
            ReadAt=t.Column<DateTime>(type:"timestamp with time zone",nullable:true)
        },constraints:t=>{t.PrimaryKey("PK_WorkshopMessageReceipts",x=>new{x.MessageId,x.RecipientId});
            t.ForeignKey("FK_WorkshopMessageReceipts_WorkshopMessages_MessageId",x=>x.MessageId,"WorkshopMessages","Id",onDelete:ReferentialAction.Cascade);
            t.ForeignKey("FK_WorkshopMessageReceipts_Users_RecipientId",x=>x.RecipientId,"Users","Id",onDelete:ReferentialAction.Restrict);});
        m.CreateIndex("IX_WorkshopMessages_SenderId","WorkshopMessages","SenderId");
        m.CreateIndex("IX_WorkshopMessages_CreatedAt","WorkshopMessages","CreatedAt");
        m.CreateIndex("IX_WorkshopMessageReceipts_RecipientId_ReadAt","WorkshopMessageReceipts",new[]{"RecipientId","ReadAt"});
        m.CreateIndex("IX_ProductionEvents_UserId_EventDate","ProductionEvents",new[]{"UserId","EventDate"});
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("WorkshopMessageReceipts");m.DropTable("WorkshopMessages");
        m.DropIndex("IX_ProductionEvents_UserId_EventDate","ProductionEvents");
    }
}

