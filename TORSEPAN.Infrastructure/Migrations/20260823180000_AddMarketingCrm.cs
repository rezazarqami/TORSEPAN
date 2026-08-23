using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260823180000_AddMarketingCrm")]
public partial class AddMarketingCrm : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name:"MarketingLeads",columns:table=>new
        {
            Id=table.Column<Guid>(type:"uuid",nullable:false),Name=table.Column<string>(type:"text",nullable:false),CompanyName=table.Column<string>(type:"text",nullable:false),Country=table.Column<string>(type:"text",nullable:false),City=table.Column<string>(type:"text",nullable:false),Website=table.Column<string>(type:"text",nullable:false),Email=table.Column<string>(type:"text",nullable:false),Phone=table.Column<string>(type:"text",nullable:false),SocialContact=table.Column<string>(type:"text",nullable:false),ContactPerson=table.Column<string>(type:"text",nullable:false),Priority=table.Column<int>(type:"integer",nullable:false),Status=table.Column<string>(type:"text",nullable:false),CooperationScore=table.Column<decimal>(type:"numeric",nullable:false),SamplePurchaseScore=table.Column<decimal>(type:"numeric",nullable:false),CurrentProducts=table.Column<string>(type:"text",nullable:false),Target=table.Column<string>(type:"text",nullable:false),Notes=table.Column<string>(type:"text",nullable:false),CreatedAt=table.Column<DateTime>(type:"timestamp with time zone",nullable:false),UpdatedAt=table.Column<DateTime>(type:"timestamp with time zone",nullable:false)
        },constraints:table=>table.PrimaryKey("PK_MarketingLeads",x=>x.Id));
        migrationBuilder.CreateTable(name:"MarketingActivities",columns:table=>new
        {
            Id=table.Column<Guid>(type:"uuid",nullable:false),MarketingLeadId=table.Column<Guid>(type:"uuid",nullable:false),Date=table.Column<DateTime>(type:"timestamp with time zone",nullable:false),Method=table.Column<string>(type:"text",nullable:false),SentText=table.Column<string>(type:"text",nullable:false),Result=table.Column<string>(type:"text",nullable:false),NextAction=table.Column<string>(type:"text",nullable:false),RegisteredBy=table.Column<string>(type:"text",nullable:false),CreatedAt=table.Column<DateTime>(type:"timestamp with time zone",nullable:false)
        },constraints:table=>{table.PrimaryKey("PK_MarketingActivities",x=>x.Id);table.ForeignKey("FK_MarketingActivities_MarketingLeads_MarketingLeadId",x=>x.MarketingLeadId,"MarketingLeads","Id",onDelete:ReferentialAction.Cascade);});
        migrationBuilder.CreateIndex("IX_MarketingActivities_MarketingLeadId","MarketingActivities","MarketingLeadId");
        migrationBuilder.CreateIndex("IX_MarketingLeads_Country_Status","MarketingLeads",new[]{"Country","Status"});
    }
    protected override void Down(MigrationBuilder migrationBuilder){migrationBuilder.DropTable("MarketingActivities");migrationBuilder.DropTable("MarketingLeads");}
}
