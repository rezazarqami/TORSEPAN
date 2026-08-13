using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260813020000_AddBowlCodeTemplates")]
public partial class AddBowlCodeTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name:"TopBowlCodeTemplate",table:"Materials",type:"character varying(20)",maxLength:20,nullable:false,defaultValue:"");
        migrationBuilder.AddColumn<string>(name:"BottomBowlCodeTemplate",table:"Materials",type:"character varying(20)",maxLength:20,nullable:false,defaultValue:"");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name:"TopBowlCodeTemplate",table:"Materials");
        migrationBuilder.DropColumn(name:"BottomBowlCodeTemplate",table:"Materials");
    }
}
