using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260920020000_AddSalesAttribution")]
public partial class AddSalesAttribution : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"SaleLeadSources",columns:table=>new{Id=table.Column<Guid>(type:"uuid",nullable:false),Name=table.Column<string>(type:"text",nullable:false),RequiresReferrer=table.Column<bool>(type:"boolean",nullable:false),IsActive=table.Column<bool>(type:"boolean",nullable:false)},constraints:table=>table.PrimaryKey("PK_SaleLeadSources",x=>x.Id));
        migrationBuilder.CreateTable(name:"SalesReferrers",columns:table=>new{Id=table.Column<Guid>(type:"uuid",nullable:false),Name=table.Column<string>(type:"text",nullable:false),IsActive=table.Column<bool>(type:"boolean",nullable:false)},constraints:table=>table.PrimaryKey("PK_SalesReferrers",x=>x.Id));
        migrationBuilder.AddColumn<Guid>(name:"SaleLeadSourceId",table:"Handpans",type:"uuid",nullable:true);
        migrationBuilder.AddColumn<Guid>(name:"SalesReferrerId",table:"Handpans",type:"uuid",nullable:true);
        migrationBuilder.CreateIndex(name:"IX_Handpans_SaleLeadSourceId",table:"Handpans",column:"SaleLeadSourceId");
        migrationBuilder.CreateIndex(name:"IX_Handpans_SalesReferrerId",table:"Handpans",column:"SalesReferrerId");
        migrationBuilder.Sql("INSERT INTO \"SaleLeadSources\" (\"Id\",\"Name\",\"RequiresReferrer\",\"IsActive\") VALUES ('11111111-1111-1111-1111-111111111111','سایت',false,true),('22222222-2222-2222-2222-222222222222','از طریق اینستاگرام',false,true),('33333333-3333-3333-3333-333333333333','از طریق یوتیوب',false,true),('44444444-4444-4444-4444-444444444444','معرف',true,true)");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name:"IX_Handpans_SaleLeadSourceId",table:"Handpans");migrationBuilder.DropIndex(name:"IX_Handpans_SalesReferrerId",table:"Handpans");
        migrationBuilder.DropColumn(name:"SaleLeadSourceId",table:"Handpans");migrationBuilder.DropColumn(name:"SalesReferrerId",table:"Handpans");
        migrationBuilder.DropTable(name:"SaleLeadSources");migrationBuilder.DropTable(name:"SalesReferrers");
    }
}
