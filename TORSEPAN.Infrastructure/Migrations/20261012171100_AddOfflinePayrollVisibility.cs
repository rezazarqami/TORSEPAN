using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;
public partial class AddOfflinePayrollVisibility : Migration {
 protected override void Up(MigrationBuilder m)=>m.AddColumn<bool>(name:"ShowMyPayroll",table:"Users",type:"boolean",nullable:false,defaultValue:false);
 protected override void Down(MigrationBuilder m)=>m.DropColumn(name:"ShowMyPayroll",table:"Users");
}
