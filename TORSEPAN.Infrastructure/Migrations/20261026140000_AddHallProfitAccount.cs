using System;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;
public partial class AddHallProfitAccount : Migration
{
 protected override void Up(MigrationBuilder migrationBuilder)
 {
  migrationBuilder.InsertData(table:"SaleProfitRules",columns:new[]{"Action","Percent","Version"},values:new object[]{100,0m,new Guid("00000000-0000-4000-8000-000000000100")});
  migrationBuilder.CreateTable(name:"HallProfitPayments",columns:table=>new{
   Id=table.Column<Guid>(type:"uuid",nullable:false),
   Amount=table.Column<decimal>(type:"numeric(18,2)",precision:18,scale:2,nullable:false),
   Currency=table.Column<int>(type:"integer",nullable:false),
   PaidAt=table.Column<DateTime>(type:"timestamp with time zone",nullable:false),
   RecordedBy=table.Column<string>(type:"character varying(300)",maxLength:300,nullable:false)
  },constraints:table=>table.PrimaryKey("PK_HallProfitPayments",x=>x.Id));
  migrationBuilder.CreateIndex(name:"IX_HallProfitPayments_PaidAt",table:"HallProfitPayments",column:"PaidAt");
 }
 protected override void Down(MigrationBuilder migrationBuilder)
 {migrationBuilder.DropTable(name:"HallProfitPayments");migrationBuilder.DeleteData(table:"SaleProfitRules",keyColumn:"Action",keyValue:100);}
}
