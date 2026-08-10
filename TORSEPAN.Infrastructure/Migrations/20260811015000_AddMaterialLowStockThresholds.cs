using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;
public partial class AddMaterialLowStockThresholds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name:"LowStockThreshold",table:"Materials",type:"integer",nullable:false,defaultValue:0);
        migrationBuilder.AddColumn<int>(name:"TopBowlLowStockThreshold",table:"Materials",type:"integer",nullable:false,defaultValue:0);
        migrationBuilder.AddColumn<int>(name:"BottomBowlLowStockThreshold",table:"Materials",type:"integer",nullable:false,defaultValue:0);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name:"LowStockThreshold",table:"Materials");
        migrationBuilder.DropColumn(name:"TopBowlLowStockThreshold",table:"Materials");
        migrationBuilder.DropColumn(name:"BottomBowlLowStockThreshold",table:"Materials");
    }
}
