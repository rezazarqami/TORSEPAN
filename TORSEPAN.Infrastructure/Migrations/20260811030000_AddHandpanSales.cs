using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;
#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;
[DbContext(typeof(TORSEPANDbContext))][Migration("20260811030000_AddHandpanSales")]
public partial class AddHandpanSales:Migration
{
 protected override void Up(MigrationBuilder m){m.AddColumn<string>(name:"BuyerName",table:"Handpans",type:"character varying(200)",maxLength:200,nullable:true);m.AddColumn<DateTime>(name:"SoldAt",table:"Handpans",type:"timestamp with time zone",nullable:true);m.AddColumn<Guid>(name:"SoldByUserId",table:"Handpans",type:"uuid",nullable:true);}
 protected override void Down(MigrationBuilder m){m.DropColumn(name:"BuyerName",table:"Handpans");m.DropColumn(name:"SoldAt",table:"Handpans");m.DropColumn(name:"SoldByUserId",table:"Handpans");}
}

